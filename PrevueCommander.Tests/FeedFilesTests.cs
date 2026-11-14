using Prevue.Commands;
using Prevue.Commands.Control;
using Xunit;

namespace PrevueCommander.Tests;

public class FeedFilesTests
{
    [Fact]
    public async Task Write_PutsEachLineInItsFile_AndLeavesOutPauses()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            BaseCommand[] commands =
            [
                new AddressCommand("*"), new PauseCommand(TimeSpan.FromSeconds(5)), new ShowLogoCommand(),
                new BoxOffCommand()
            ];
            var data = Path.Combine(directory, "feed.bin");
            var control = Path.Combine(directory, "control.bin");

            var counts = await FeedFiles.WriteAsync(commands, data, control, false, TextWriter.Null,
                TestContext.Current.CancellationToken);

            Assert.Equal((2, 1, 0), counts);
            byte[] expected = [.. commands[0].Render(), .. commands[3].Render()];
            Assert.Equal(expected, await File.ReadAllBytesAsync(data, TestContext.Current.CancellationToken));
            Assert.Equal(commands[2].Render(), await File.ReadAllBytesAsync(control, TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task Write_WithoutAControlFile_LeavesOutTheControlLine()
    {
        var data = Path.GetTempFileName();
        try
        {
            var counts = await FeedFiles.WriteAsync([new ShowLogoCommand(), new BoxOffCommand()], data, null, false,
                TextWriter.Null, TestContext.Current.CancellationToken);
            Assert.Equal((1, 0, 1), counts);
            Assert.Equal(new BoxOffCommand().Render(), await File.ReadAllBytesAsync(data, TestContext.Current.CancellationToken));
        }
        finally
        {
            File.Delete(data);
        }
    }
}
