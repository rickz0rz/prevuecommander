using System.Text;
using Prevue.Commands;
using Prevue.Commands.Model;
using Xunit;

namespace PrevueCommander.Tests;

public class CommandTests
{
    private static string Hex(BaseCommand command) => Convert.ToHexString(command.Render());

    [Fact]
    public void SaveBoxOffAndReset_MatchTheFramesThatEsqChecks()
    {
        Assert.Equal("55AA2500DA", Hex(new SaveCommand()));
        Assert.Equal("55AABBBB00FF", Hex(new BoxOffCommand()));
        Assert.Equal("55AA5200AD", Hex(new ResetCommand()));
    }

    [Fact]
    public void Address_HasTheTargetAndTheChecksum()
    {
        Assert.Equal("55AA412A0094", Hex(new AddressCommand("*")));
        Assert.Throws<ArgumentException>(() => new AddressCommand(new string('A', 17)));
    }

    [Fact]
    public void Configuration_HasTimeZone6ByDefault_AsTheEsquireFeed()
    {
        byte[] fields = [.. "BE3366N"u8, 0x01, 0x01, .. "6YYNNNYANN"u8, 0x00];
        var bytes = new ConfigurationCommand().Render();
        Assert.Equal(fields, bytes[3..^2]);
        Assert.Equal(21, bytes.Length - 4); // ESQ reads exactly 20 bytes and the NUL.
    }

    [Fact]
    public void NewLookConfiguration_DefaultRecord_IsTheRecordOfTheOriginalDataFiles()
    {
        byte[] expected =
            [0x00, 0x00, 0x36, .. "2C010808GNAE01NNNNNNL2906YYY233606015100YNYC"u8, 0x8E, .. "8SNNNN2"u8];
        var bytes = new NewLookConfigurationCommand().Render();
        Assert.Equal(expected, bytes[3..^2]);
    }

    [Fact]
    public void NewLookConfiguration_UsesTheFieldsOfThePlaybook()
    {
        var record = NewLookConfigurationCommand.Record(new NewLookConfiguration
            { DisplayMode = 'S', MilitaryTime = 'Y', MinutesToDisplayPPVPostStart = 120 });
        Assert.Equal((byte)'S', record[8]);
        Assert.Equal((byte)'Y', record[41]);
        Assert.Equal("120"u8.ToArray(), record[34..37]);
    }

    [Fact]
    public void Clock_SendsTheTimeWithoutAChange()
    {
        var bytes = new ClockCommand(new DateTime(2026, 1, 15, 16, 30, 5)).Render();
        Assert.Equal(new byte[] { 4, 0, 14, 126, 16, 30, 5, 0 }, bytes[3..11]);
    }

    [Fact]
    public void LocalAd_StartsAt1AndConvertsTokens()
    {
        var bytes = new LocalAdCommand(1, "%COLOR%%BLACK%%CYAN%Hi%CENTER%100%%").Render();
        Assert.Equal((byte[])[1, 0x03, (byte)'2', (byte)'5', (byte)'H', (byte)'i', 0x18, .. "100%"u8],
            bytes[3..^2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => new LocalAdCommand(0, "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LocalAdCommand(46, "x"));
    }

    [Fact]
    public void ChannelLineUp_HasTheLayoutOfTheEsquireFeed()
    {
        var day = new DateOnly(2026, 11, 12); // Day 316, group code 60.
        var command = new ChannelLineUpCommand(day, [new Channel("WDIV", "4.1", "WDIVDT", Premium: true)]);
        byte[] expected =
        [
            60, 0x12, 0x03, .. "WDIV"u8, 0x11, .. "4.1"u8, 0x14, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01,
            .. "WDIVDT"u8
        ];
        Assert.Equal(expected, command.Render()[3..^2]);
    }

    [Fact]
    public void ChannelProgram_HasSlotGroupSourceAttributeAndText()
    {
        var command = new ChannelProgramCommand(new DateOnly(2026, 11, 12), 33, "WDIV", true, "\"Movie\" (1999)");
        byte[] expected = [33, 60, .. "WDIV"u8, 0x12, 0x03, .. "\"Movie\" (1999)"u8];
        Assert.Equal(expected, command.Render()[3..^2]);
    }

    [Fact]
    public void ChannelProgram_RefusesSourceNamesThatEsqWouldMatchAsAPattern()
    {
        Assert.Throws<ArgumentException>(() => new ChannelProgramCommand(new DateOnly(2026, 1, 1), 1, "A*", false, "x"));
        Assert.Throws<ArgumentException>(() =>
            new ChannelProgramCommand(new DateOnly(2026, 1, 1), 1, "TOOLONG", false, "x"));
    }

    [Fact]
    public void Utility_SendsAsciiText()
    {
        Assert.Equal("33c"u8.ToArray(), new UtilityCommand(UtilityAction.ReloadBanner).Render()[3..^2]);
    }

    [Fact]
    public void FileTransfer_SendsHeaderBlocksAndEnd()
    {
        var content = Enumerable.Range(0, 300).Select(i => (byte)i).ToArray();
        var bytes = new FileTransferCommand("DF0:Logos/AD.IFF", content).Render();

        var header = Encoding.ASCII.GetBytes("DF0:Logos/AD.IFF");
        var headerLength = 3 + header.Length + 2;
        Assert.Equal(header, bytes[3..(3 + header.Length)]);

        var first = bytes[headerLength..(headerLength + 3 + 2 + 255 + 1)];
        Assert.Equal(new byte[] { 0x55, 0xAA, (byte)'H', 0, 255 }, first[..5]);
        Assert.Equal(0, first.Aggregate(0, (x, b) => x ^ b)); // The XOR of a frame and its checksum is 0.

        var end = bytes[^6..];
        Assert.Equal(new byte[] { 0x55, 0xAA, (byte)'H', 2, 0 }, end[..5]);
        Assert.Equal(0, end.Aggregate(0, (x, b) => x ^ b));
        Assert.Throws<ArgumentException>(() => new FileTransferCommand("AD.IFF", content));
    }

    [Fact]
    public void EveryFrame_HasAChecksumThatMakesTheXorZero()
    {
        BaseCommand[] commands =
        [
            new AddressCommand("GA24005"), new ConfigurationCommand(), new NewLookConfigurationCommand(),
            new ClockCommand(DateTime.Now), new LocalAdCommand(3, "Ad"), new ResetLocalAdsCommand()
        ];
        foreach (var command in commands)
            Assert.Equal(0, command.Render().Aggregate(0, (x, b) => x ^ b));
    }
}
