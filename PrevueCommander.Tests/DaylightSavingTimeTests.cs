using Prevue.Commands;
using Xunit;

namespace PrevueCommander.Tests;

public class DaylightSavingTimeTests
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    [Fact]
    public void FindWindow_GivesTheLocalTimesAfterTheChanges()
    {
        var window = DaylightSavingTimeCommand.FindWindow(2021, Eastern);
        Assert.Equal((new DateTime(2021, 3, 14, 3, 0, 0), new DateTime(2021, 11, 7, 1, 0, 0)), window);
    }

    [Fact]
    public void FindWindow_IsNullForAZoneWithoutDaylightSavingTime()
    {
        Assert.Null(DaylightSavingTimeCommand.FindWindow(2021, TimeZoneInfo.Utc));
    }

    [Fact]
    public void Command_HasTheFormatOfDstDatOnTheOriginalDrive()
    {
        // dst.dat of the original drive: "g3:" 04 "202107203:00" 13 "202131001:00".
        var (start, end) = DaylightSavingTimeCommand.FindWindow(2021, Eastern)!.Value;
        var bytes = new DaylightSavingTimeCommand('3', start, end).Render();
        byte[] expected = [(byte)'g', (byte)'3', .. "27"u8, 0x04, .. "202107203:00"u8, 0x13, .. "202131001:00"u8, 0];
        Assert.Equal(expected, bytes[2..^1]);
    }
}
