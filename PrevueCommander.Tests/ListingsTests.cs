using Prevue.Commands;
using Prevue.Commands.Listings;
using Prevue.Core;
using Xunit;

namespace PrevueCommander.Tests;

public class ListingsTests
{
    [Theory]
    [InlineData("2026-11-12T05:29", "2026-11-11")]
    [InlineData("2026-11-12T05:30", "2026-11-12")]
    [InlineData("2026-11-12T00:17", "2026-11-11")]
    [InlineData("2026-11-12T23:59", "2026-11-12")]
    public void CurrentBroadcastDay_ChangesAt0530(string time, string day)
    {
        Assert.Equal(DateOnly.Parse(day), BroadcastDay.Current(DateTime.Parse(time)));
    }

    [Theory]
    [InlineData("2026-09-12", 255)]
    [InlineData("2026-09-13", 0)] // Day 256
    [InlineData("2026-11-12", 60)] // Day 316
    public void GroupCode_IsTheDayOfTheYearModulo256(string day, int code)
    {
        Assert.Equal(code, BroadcastDay.GroupCode(DateOnly.Parse(day)));
    }

    [Theory]
    [InlineData("2026-11-12T05:00", 1)]
    [InlineData("2026-11-12T05:29", 1)]
    [InlineData("2026-11-12T08:15", 7)]
    [InlineData("2026-11-12T20:00", 31)]
    [InlineData("2026-11-13T04:30", 48)]
    [InlineData("2026-11-12T03:00", 1)]
    public void Slot_IsTheHalfHourInWhichTheTimeFalls(string time, int slot)
    {
        Assert.Equal(slot, BroadcastDay.Slot(new DateOnly(2026, 11, 12), DateTime.Parse(time)));
    }

    [Fact]
    public void Build_SendsTheLineupAndTheProgramsOfTwoDays()
    {
        var day = new DateOnly(2026, 11, 12);
        var start = BroadcastDay.StartOf(day);
        GuideChannel[] channels =
        [
            new("4", "WDIVDT",
            [
                new GuideProgram(start.AddHours(-1), start.AddHours(1), "Early"), // Starts before the day.
                new GuideProgram(start.AddHours(3).AddMinutes(15), start.AddHours(4), "News"),
                new GuideProgram(start.AddHours(3).AddMinutes(20), start.AddHours(4), "Lost"), // Same slot.
                new GuideProgram(start.AddDays(1).AddHours(1), start.AddDays(1).AddHours(2), "Tomorrow")
            ])
        ];

        var commands = ListingsBuilder.Build(channels, start.AddHours(10));

        var lineups = commands.OfType<ChannelLineUpCommand>().ToList();
        Assert.Equal([day, day.AddDays(1)], lineups.Select(l => l.Day));
        var programs = commands.OfType<ChannelProgramCommand>().ToList();
        Assert.Equal(
        [
            (day, 1, "Early"), (day, 7, "( 8:15) News"), (day.AddDays(1), 3, "Tomorrow")
        ], programs.Select(p => (p.Day, p.Slot, p.Text)));
        Assert.All(programs, p => Assert.Equal("WDIVDT", p.SourceName));
    }

    [Fact]
    public void Build_LimitsTheLineupTo200Channels()
    {
        var channels = Enumerable.Range(1, 250).Select(i => new GuideChannel($"{i}", $"C{i}", []));
        var lineup = ListingsBuilder.Build(channels, DateTime.Now).OfType<ChannelLineUpCommand>().First();
        Assert.Equal(200, lineup.Channels.Count);
    }

    [Fact]
    public void SourceNames_AreShortAndDifferent()
    {
        Assert.Equal(["WXYZDT", "WXYZD2", "KABC", "CH", "CH2"],
            ListingsBuilder.SourceNames(["WXYZDT", "WXYZDT2", "k*abc", "", "?"]));
    }

    [Fact]
    public void ProgramText_HasTheFormatOfEsquire()
    {
        Assert.Equal("\"Casablanca\" (1942) A cafe owner meets an old love. (PG) |",
            ProgramText.Movie("Casablanca (1942)", 0, "A cafe owner meets an old love.", "PG", true));
        Assert.Equal("\"The 'Thing'\" (1982)", ProgramText.Movie("The \"Thing\"", 1982));
        Assert.Equal("NFL Football: Detroit Lions at Carolina Panthers",
            ProgramText.Show("NFL Football", false, "Detroit Lions at Carolina Panthers"));
        Assert.Equal("This/That |", ProgramText.Show("This|That", true));
        Assert.Equal("(TV-PG)", ProgramText.RatingToken("TVPG"));
        Assert.Null(ProgramText.RatingToken("Unrated"));
    }

    [Fact]
    public void ChannelNumberComparer_SortsMajorThenMinor()
    {
        string[] numbers = ["38", "2.10", "", "4", "2.1", "6-1"];
        Assert.Equal(["2.1", "2.10", "4", "6-1", "38", ""],
            numbers.SortByChannelNumber(SortMode.Ascending, n => n));
    }
}
