using Prevue.Commands;
using PrevueCommander.Model.PlaybookCommands;
using Xunit;

namespace PrevueCommander.Tests;

public class PlaybookTests
{
    private static readonly PlaybookContext Context =
        new(new DateTime(2026, 11, 12, 20, 0, 0), Directory.GetCurrentDirectory(), TextWriter.Null);

    [Fact]
    public void ExamplePlaybook_IsValid()
    {
        var playbook = PlaybookLoader.Parse(PlaybookLoader.Example());
        Assert.Equal(5400, playbook.Configuration.Port);
        Assert.Contains(playbook.Commands, c => c is XmlTvGuideDataImportPlaybookCommand);
        Assert.Equal('6', playbook.Commands.OfType<ConfigurationPlaybookCommand>().Single().Configuration.TimeZone);
    }

    [Fact]
    public async Task Playbook_SettingsReachTheCommands()
    {
        var playbook = PlaybookLoader.Parse("""
            commands:
            - !!clock
              date: 2020-11-01T16:00
            - !!utility
              action: ReloadBanner
            - !!newLookConfiguration
              configuration:
                displayMode: S
            - !!reset {}
            - !!localAds
              ads: [One, Two]
            """);

        var commands = new List<BaseCommand>();
        foreach (var command in playbook.Commands)
            commands.AddRange(await command.TransformAsync(Context, CancellationToken.None));

        Assert.Equal(new DateTime(2020, 11, 1, 16, 0, 0), commands.OfType<ClockCommand>().Single().DateTime);
        Assert.Equal(UtilityAction.ReloadBanner, commands.OfType<UtilityCommand>().Single().Action);
        Assert.Equal((byte)'S', commands.OfType<NewLookConfigurationCommand>().Single().Render()[3 + 3 + 8]);
        Assert.IsType<AddressCommand>(commands[3]);
        Assert.IsType<ResetCommand>(commands[4]);
        Assert.Equal([1, 2], commands.OfType<LocalAdCommand>().Select(c => c.Number));
    }

    [Fact]
    public async Task Clock_WithoutDate_UsesTheTimeOfTheRun()
    {
        var commands = await new ClockPlaybookCommand().TransformAsync(Context, CancellationToken.None);
        Assert.Equal(Context.Now, Assert.IsType<ClockCommand>(Assert.Single(commands)).DateTime);
    }

    [Fact]
    public async Task FileTransfer_InADirectory_MakesTheRamDirectoryFirst()
    {
        var source = Path.GetTempFileName();
        try
        {
            var command = new FileTransferPlaybookCommand { Source = source, Target = "DF0:Logos/AD.IFF" };
            var commands = await command.TransformAsync(Context, CancellationToken.None);
            Assert.Equal(
                ["BoxOffCommand", "AddressCommand", "UtilityCommand", "UtilityCommand", "BoxOffCommand",
                    "AddressCommand", "FileTransferCommand"],
                commands.Select(c => c.GetType().Name));
            Assert.Equal("32MakeDir RAM:Logos", ((UtilityCommand)commands[2]).Text);
        }
        finally
        {
            File.Delete(source);
        }
    }
}
