using Prevue.Commands;
using Prevue.Commands.Listings;
using PrevueCommander.Model.PlaybookCommands;
using PrevueCommander.Model.PlaybookCommands.CommandObjects;
using Xunit;

namespace PrevueCommander.Tests;

public class WatchTests
{
    private static readonly DateTime Evening = new(2026, 11, 13, 18, 10, 0);

    private static GuideChannel Channel(string callSign, string title, DateTime start) =>
        new("2", callSign, [new GuideProgram(start, start.AddMinutes(30), title)]);

    private static List<BaseCommand> Listings(DateTime now, params GuideChannel[] channels) =>
        ListingsBuilder.Build(channels, now);

    [Fact]
    public void SameListings_HaveNoChanges()
    {
        var state = new ListingsState();
        state.Remember(Listings(Evening, Channel("TESTA", "News", Evening.Date.AddHours(18))));

        Assert.Empty(state.Changes(Listings(Evening, Channel("TESTA", "News", Evening.Date.AddHours(18)))));
    }

    [Fact]
    public void AChangedProgram_SendsOnlyThatProgram()
    {
        var state = new ListingsState();
        state.Remember(Listings(Evening, Channel("TESTA", "News", Evening.Date.AddHours(18))));

        var changes = state.Changes(Listings(Evening, Channel("TESTA", "Weather", Evening.Date.AddHours(18))));

        var program = Assert.IsType<ChannelProgramCommand>(Assert.Single(changes));
        Assert.Equal("Weather", program.Text);
        Assert.Empty(state.Changes(Listings(Evening, Channel("TESTA", "Weather", Evening.Date.AddHours(18)))));
    }

    [Fact]
    public void AChangedLineUp_SendsTheLineUpAndAllThePrograms()
    {
        var state = new ListingsState();
        var start = Evening.Date.AddHours(18);
        state.Remember(Listings(Evening, Channel("TESTA", "News", start)));

        var changes = state.Changes(Listings(Evening, Channel("TESTA", "News", start), Channel("TESTB", "Sports", start)));

        // Both days have a new lineup, so each day sends its lineup and the programs of both channels.
        Assert.Equal(2, changes.OfType<ChannelLineUpCommand>().Count());
        Assert.Contains(changes.OfType<ChannelProgramCommand>(), p => p.Text == "News");
        Assert.Contains(changes.OfType<ChannelProgramCommand>(), p => p.Text == "Sports");
    }

    [Fact]
    public void ANewDay_SendsItsLineUpAndPrograms_AndTheOldDayIsForgotten()
    {
        var state = new ListingsState();
        var channel = Channel("TESTA", "News", Evening.Date.AddHours(18));
        state.Remember(Listings(Evening, channel));

        // After 5:30 AM, the current day is the old next day, and a new next day starts.
        var morning = Evening.Date.AddDays(1).AddHours(6);
        var changes = state.Changes(Listings(morning, channel));

        var newDay = DateOnly.FromDateTime(Evening).AddDays(2);
        Assert.Equal([newDay], changes.OfType<ChannelLineUpCommand>().Select(c => c.Day));
        Assert.Equal([DateOnly.FromDateTime(Evening).AddDays(1), newDay], state.Days);
    }

    [Fact]
    public void WithoutALineUp_OnlyTheChangedProgramsAreSent()
    {
        var state = new ListingsState();
        var start = Evening.Date.AddHours(18);
        state.Remember(ListingsBuilder.Build([Channel("TESTA", "News", start)], Evening, sendChannelLineUp: false));

        var changes = state.Changes(ListingsBuilder.Build([Channel("TESTA", "Weather", start)], Evening,
            sendChannelLineUp: false));

        Assert.IsType<ChannelProgramCommand>(Assert.Single(changes));
    }

    [Fact]
    public async Task Watcher_SendsASessionWithTheChanges_AndNothingWithoutChanges()
    {
        var file = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(file, Xml("News"), TestContext.Current.CancellationToken);
            var import = new XmlTvGuideDataImportPlaybookCommand { XmlTvFiles = [new XmlTvFile { Path = file }] };
            PlaybookContext ContextAt(DateTime time) => new(time, Path.GetTempPath(), TextWriter.Null);
            var first = await import.TransformAsync(ContextAt(Evening), TestContext.Current.CancellationToken);

            var sent = new List<BaseCommand>();
            var watcher = new GuideWatcher([import], ContextAt, () => Evening,
                (command, _) =>
                {
                    sent.Add(command);
                    return Task.CompletedTask;
                }, TextWriter.Null);
            watcher.Start([new AddressCommand("*"), new ConfigurationCommand(), .. first, new SaveCommand(),
                new BoxOffCommand()]);

            Assert.Equal(0, await watcher.UpdateAsync(TestContext.Current.CancellationToken));
            Assert.Empty(sent);

            await File.WriteAllTextAsync(file, Xml("Weather"), TestContext.Current.CancellationToken);
            Assert.Equal(1, await watcher.UpdateAsync(TestContext.Current.CancellationToken));
            Assert.Equal([typeof(AddressCommand), typeof(ConfigurationCommand), typeof(ChannelProgramCommand),
                typeof(SaveCommand), typeof(BoxOffCommand)], sent.Select(c => c.GetType()));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task Watcher_KeepsRunning_WhenTheGuideCannotBeRead()
    {
        var import = new XmlTvGuideDataImportPlaybookCommand
        {
            XmlTvFiles = [new XmlTvFile { Path = "/this/file/does/not/exist.xml" }]
        };
        var log = new StringWriter();
        var watcher = new GuideWatcher([import], time => new PlaybookContext(time, "/", TextWriter.Null),
            () => Evening, (_, _) => Task.CompletedTask, log);

        Assert.Equal(0, await watcher.UpdateAsync(TestContext.Current.CancellationToken));
        Assert.Contains("The guide cannot be read", log.ToString());
    }

    private static string Xml(string title) => $"""
        <?xml version="1.0"?>
        <tv>
          <channel id="c1"><display-name>2 TESTA</display-name></channel>
          <programme start="20261113180000" stop="20261113183000" channel="c1"><title>{title}</title></programme>
        </tv>
        """;
}
