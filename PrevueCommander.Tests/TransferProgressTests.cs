using Prevue.Commands;
using PrevueCommander.Model;
using Xunit;

namespace PrevueCommander.Tests;

public class TransferProgressTests
{
    private static readonly DateTime Start = new(2026, 11, 13, 18, 0, 0);

    [Fact]
    public void Time_IsTheBytesAtTheBaudRate_AndTheDelay()
    {
        // The address command has 6 bytes: 60 bits at 2400 baud take 25 ms.
        var progress = new TransferProgress([], new Configuration { BaudRate = 2400, CommandDelay = 100 },
            TextWriter.Null, TimeSpan.Zero);
        Assert.Equal(TimeSpan.FromMilliseconds(125), progress.Time(new AddressCommand("*")));
        Assert.Equal(TimeSpan.FromSeconds(3), progress.Time(new PauseCommand(TimeSpan.FromSeconds(3))));
    }

    [Fact]
    public void Time_AtFullSpeed_IsOnlyTheDelay()
    {
        var progress = new TransferProgress([], new Configuration { BaudRate = 0, CommandDelay = 0 },
            TextWriter.Null, TimeSpan.Zero);
        Assert.Equal(TimeSpan.Zero, progress.Time(new AddressCommand("*")));
    }

    [Fact]
    public void Start_And_Sent_ShowTheBytesAndTheExpectedEnd()
    {
        // 240 bytes take 1 second at 2400 baud. Each local ad here has 240 bytes.
        var ad = new LocalAdCommand(1, new string('x', 234));
        Assert.Equal(240, ad.Render().Length);
        BaseCommand[] commands = [ad, ad, ad, ad];
        var log = new StringWriter();
        var progress = new TransferProgress(commands, new Configuration { BaudRate = 2400, CommandDelay = 0 }, log,
            TimeSpan.Zero, () => Start);

        progress.Start();
        progress.Sent(ad);

        Assert.Equal(960, progress.TotalBytes);
        Assert.Equal(TimeSpan.FromSeconds(4), progress.TotalTime);
        var lines = log.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("The run sends 960 bytes. It takes about 4 s. Expected end: 18:00:04.", lines[0]);
        Assert.Equal("Sent 240 of 960 bytes (25%). Expected end: 18:00:03.", lines[1]);
    }

    [Theory]
    [InlineData(0, "0 s")]
    [InlineData(59.2, "1 min 0 s")]
    [InlineData(65, "1 min 5 s")]
    [InlineData(3903, "1 h 5 min 3 s")]
    public void Format_ShowsHoursMinutesAndSeconds(double seconds, string text) =>
        Assert.Equal(text, TransferProgress.Format(TimeSpan.FromSeconds(seconds)));
}
