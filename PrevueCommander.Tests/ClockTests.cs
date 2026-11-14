using Prevue.Commands;
using PrevueCommander.Model.PlaybookCommands;
using PrevueCommander.Server;
using Xunit;

namespace PrevueCommander.Tests;

public class ClockTests
{
    private static readonly DateTime Summer = new(2026, 7, 4, 15, 42, 0);
    private static readonly DateTime Winter = new(2026, 1, 10, 3, 21, 0);

    [Fact]
    public void ClockCommand_SendsTheDaylightSavingTimeFlagLast()
    {
        var command = new ClockCommand(Summer);
        Assert.Equal(Summer.IsDaylightSavingTime() ? (byte)1 : (byte)0, command.Render()[^3]);
        Assert.Equal(Summer.IsDaylightSavingTime(), command.DaylightSavingTime);
    }

    [Fact]
    public void DstWarning_ForASummerClockWithoutAWindow()
    {
        if (!Summer.IsDaylightSavingTime())
            Assert.Skip("The time zone of this computer has no daylight saving time.");

        Assert.NotNull(ClockPlaybookCommand.DstWarning([new ClockCommand(Summer)]));
        Assert.Null(ClockPlaybookCommand.DstWarning([new ClockCommand(Summer),
            new DaylightSavingTimeCommand('3', new DateTime(2026, 3, 8, 3, 0, 0), new DateTime(2026, 11, 1, 1, 0, 0))]));
    }

    [Fact]
    public void DstWarning_NotForAWinterClock() =>
        Assert.Null(ClockPlaybookCommand.DstWarning([new ClockCommand(Winter)]));

    [Fact]
    public async Task Server_SendsTheDstWindowBeforeTheClock()
    {
        var sink = new Sink();
        var jobs = new JobQueue(sink, _ => TimeSpan.FromSeconds(1), () => Summer, TextWriter.Null);
        var hold = new TopHalfHold(jobs, () => Summer, TextWriter.Null, TestContext.Current.CancellationToken);
        var parts = new ServerParts(() => new PlaybookContext(Summer, Path.GetTempPath(), TextWriter.Null),
            new SessionTemplate(), _ => { }, _ => (false, 0), 110);
        var server = new ControlServer(parts, jobs, hold, () => Summer, TextWriter.Null);

        var (status, _) = await server.HandleAsync("POST", "/prevue/data/clock", """{"date": "2026-07-04T15:42"}""",
            TestContext.Current.CancellationToken);
        using var stop = new CancellationTokenSource();
        var run = jobs.RunAsync(stop.Token);
        while (jobs.Waiting() != (0, 0))
            await Task.Delay(10, TestContext.Current.CancellationToken);
        await stop.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);

        Assert.Equal(202, status);
        var batch = Assert.Single(sink.Batches);
        var clock = batch.ToList().FindIndex(c => c is ClockCommand);
        var window = batch.ToList().FindIndex(c => c is DaylightSavingTimeCommand);
        if (DaylightSavingTimeCommand.FindWindow(2026, TimeZoneInfo.Local) == null)
            Assert.Equal(-1, window);
        else
            Assert.InRange(window, 0, clock - 1);
    }

    private sealed class Sink : ICommandSink
    {
        public List<IReadOnlyList<BaseCommand>> Batches { get; } = [];

        public Task SendBatchAsync(IReadOnlyList<BaseCommand> commands, Action<BaseCommand>? sent,
            CancellationToken cancellationToken)
        {
            Batches.Add(commands);
            return Task.CompletedTask;
        }
    }
}
