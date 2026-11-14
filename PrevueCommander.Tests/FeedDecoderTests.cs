using Prevue.Commands;
using Prevue.Commands.Control;
using Prevue.Commands.Model;
using Xunit;

namespace PrevueCommander.Tests;

public class FeedDecoderTests
{
    private static byte[] Feed(params BaseCommand[] commands) => commands.SelectMany(c => c.Render()).ToArray();

    private static string Field(DecodedCommand command, string name) =>
        command.Fields.Single(f => f.Name == name).Value;

    [Fact]
    public void Data_SplitsTheCommands_AndChecksTheChecksums()
    {
        var day = new DateOnly(2026, 11, 13);
        var feed = Feed(new AddressCommand("*"), new ConfigurationCommand(),
            new ChannelLineUpCommand(day, [new Channel("WXYZ", "7", "WXYZ", true)]),
            new ChannelProgramCommand(day, 3, "WXYZ", true, "\"Movie\" (1990)"), new BoxOffCommand());

        var commands = FeedDecoder.DecodeData(feed).ToList();

        Assert.Equal(["'A' address", "'F' status packet", "'C' channel lineup", "'P' program", "box off"],
            commands.Select(c => c.Name));
        Assert.All(commands, c => Assert.True(c.ChecksumIsCorrect));
        Assert.Equal(feed.Length, commands.Sum(c => c.Bytes.Length));
        Assert.Equal("\"*\"", Field(commands[0], "target"));
        Assert.Equal("\"6\"", Field(commands[1], "time zone"));
        Assert.Contains("number \"7\", call letters \"WXYZ\", attribute $03", Field(commands[2], "channel 1"));
        Assert.Equal("3", Field(commands[3], "slot"));
        Assert.Equal("$03 (movie)", Field(commands[3], "attribute"));
        Assert.Equal("\"\\\"Movie\\\" (1990)\"", Field(commands[3], "text"));
    }

    [Fact]
    public void Data_KeepsACommandWithANulInItsPayload()
    {
        // On a Sunday in January, the first two bytes of the clock are 0.
        var feed = Feed(new ClockCommand(new DateTime(2023, 1, 1, 0, 0, 0)), new BoxOffCommand());

        var commands = FeedDecoder.DecodeData(feed).ToList();

        Assert.Equal(2, commands.Count);
        Assert.True(commands[0].ChecksumIsCorrect);
        Assert.Equal("2023-01-01 00:00:00", Field(commands[0], "time"));
        Assert.Equal("Sunday", Field(commands[0], "day of the week"));
    }

    [Fact]
    public void Data_ShowsTheFileTransferBlocks()
    {
        var feed = Feed(new FileTransferCommand("DF0:test.txt", [1, 2, 3]));

        var commands = FeedDecoder.DecodeData(feed).ToList();

        Assert.Equal(["'H' file transfer", "'H' file transfer block", "'H' file transfer block"],
            commands.Select(c => c.Name));
        Assert.Equal("\"DF0:test.txt\"", Field(commands[0], "path"));
        Assert.Equal("3", Field(commands[1], "length"));
        Assert.Equal("1", Field(commands[2], "sequence"));
        Assert.Equal("0", Field(commands[2], "length"));
    }

    [Fact]
    public void Data_FindsABadChecksum_AndJunk()
    {
        var bad = new AddressCommand("*").Render();
        bad[^1] ^= 0xFF;
        byte[] feed = [0x01, 0x02, .. bad, .. new BoxOffCommand().Render()];

        var commands = FeedDecoder.DecodeData(feed).ToList();

        Assert.Equal(["junk", "'A' address", "box off"], commands.Select(c => c.Name));
        Assert.False(commands[1].ChecksumIsCorrect);
        Assert.True(commands[2].ChecksumIsCorrect);
        Assert.Equal(2, FeedDecoder.Write(commands, TextWriter.Null, false));
    }

    [Fact]
    public void Control_SplitsThePackets()
    {
        var packets = Promo.Commands(new PromoBox("Seinfeld", Brush: "AT"), null);
        var feed = packets.SelectMany(p => p.Render()).ToArray();

        var commands = FeedDecoder.DecodeControl(feed).ToList();

        Assert.Equal(packets.Count, commands.Count);
        Assert.All(commands, c => Assert.True(c.ChecksumIsCorrect));
        Assert.Equal("17", Field(commands[1], "type"));
        Assert.Equal("\"Seinfeld\"", Field(commands[1], "body"));
    }

    [Fact]
    public void Control_FindsABadChecksum()
    {
        var packet = new ShowLogoCommand().Render();
        packet[^1] ^= 0xFF;

        var command = Assert.Single(FeedDecoder.DecodeControl(packet));

        Assert.False(command.ChecksumIsCorrect);
    }

    [Fact]
    public void CommandLine_ReadsDecode()
    {
        var options = CommandLine.Parse(["decode", "--control", "feed.bin"]);
        Assert.Equal("feed.bin", options.DecodePath);
        Assert.True(options.DecodeControl);
        Assert.Throws<CommandLineException>(() => CommandLine.Parse(["decode"]));
        Assert.Throws<CommandLineException>(() => CommandLine.Parse(["decode", "--port", "1", "feed.bin"]));
    }
}
