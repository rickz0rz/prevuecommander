using Prevue.Commands;
using Prevue.Commands.Control;
using Prevue.Commands.Listings;
using PrevueCommander.Model;
using PrevueCommander.Model.PlaybookCommands;
using Xunit;

namespace PrevueCommander.Tests;

public class AutoPromoTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 18, 10, 0);

    private static GuideProgram Show(int hour, int minute, string text, bool movie = false)
    {
        var start = Now.Date.AddHours(hour).AddMinutes(minute);
        return new GuideProgram(start, start.AddMinutes(30), text, movie);
    }

    /// <summary>KTIV (2) has News at 18:00, Seinfeld at 18:30 and a movie at 19:00. HBO (5, premium) has Golf at 18:30.</summary>
    private static List<BaseCommand> Listings() => ListingsBuilder.Build(
    [
        new GuideChannel("2", "KTIVDT", [Show(18, 0, "News |"), Show(18, 30, "Seinfeld |"),
            Show(19, 0, ProgramText.Movie("Casablanca", 1942, "A cafe owner."), true)]),
        new GuideChannel("5", "HBO", [Show(18, 30, "( 6:35) Golf")], Premium: true)
    ], Now);

    [Fact]
    public void Candidates_StartAfterTheCurrentHalfHour_InTheOrderOfTimeAndLineUp()
    {
        var candidates = AutoPromoPicker.Candidates(Listings(), Now, new AutoPromoCriteria());

        Assert.Equal(["Seinfeld", "Golf", "Casablanca"], candidates.Select(c => c.Title));
        Assert.Equal("KTIVDT", candidates[0].CallLetters);
        Assert.Equal("2", candidates[0].Number);
    }

    [Fact]
    public void Candidates_WithIncludeNow_HaveTheCurrentHalfHour() =>
        Assert.Equal("News", AutoPromoPicker.Candidates(Listings(), Now, new AutoPromoCriteria(IncludeNow: true))[0].Title);

    [Fact]
    public void Candidates_WithinOneHalfHour_HaveOnlyTheNextHalfHour() =>
        Assert.Equal(["Seinfeld", "Golf"],
            AutoPromoPicker.Candidates(Listings(), Now, new AutoPromoCriteria(WithinHours: 0.5)).Select(c => c.Title));

    [Theory]
    [InlineData(true, null, null, null, "Casablanca")]
    [InlineData(false, null, null, null, "Seinfeld,Golf")]
    [InlineData(null, true, null, null, "Golf")]
    [InlineData(null, null, "5", null, "Golf")]
    [InlineData(null, null, "ktiv*", null, "Seinfeld,Casablanca")]
    [InlineData(null, null, null, "SEIN", "Seinfeld")]
    public void Candidates_FollowTheFilters(bool? movies, bool? premium, string? channel, string? title,
        string expected)
    {
        var criteria = new AutoPromoCriteria(movies, premium, channel == null ? null : [channel],
            title == null ? null : [title]);
        Assert.Equal(expected, string.Join(",", AutoPromoPicker.Candidates(Listings(), Now, criteria).Select(c => c.Title)));
    }

    [Theory]
    [InlineData("Seinfeld |", "Seinfeld", "Seinfeld")]
    [InlineData("( 8:15) News |", "News", "News")]
    [InlineData("Jeopardy! In Stereo |", "Jeopardy!", "Jeopardy!")]
    [InlineData("\"Casablanca\" (1942) A cafe owner. (PG) |", "Casablanca", "\"Casablanca\"")]
    public void Titles_ComeFromTheProgramText(string text, string title, string search) =>
        Assert.Equal((title, search), AutoPromoPicker.Titles(text));

    [Fact]
    public void Pick_DoesNotRepeatARecentTitle_UntilAllTitlesWereShown()
    {
        var picker = new AutoPromoPicker();
        var criteria = new AutoPromoCriteria(Repeat: 10);

        var picks = Enumerable.Range(0, 4).Select(_ => picker.Pick(Listings(), Now, criteria)!.Title).ToList();

        // After the three titles, the oldest title shows again.
        Assert.Equal(["Seinfeld", "Golf", "Casablanca", "Seinfeld"], picks);
    }

    [Fact]
    public void Pick_WithoutAProgramThatFits_IsNull() =>
        Assert.Null(new AutoPromoPicker().Pick(Listings(), Now, new AutoPromoCriteria(Titles: ["Nothing"])));

    [Fact]
    public async Task Promoter_SendsAPromoForTheProgram_OnItsChannel()
    {
        var sent = new List<BaseCommand>();
        var promoter = new AutoPromoter(new AutoPromoSettings { Brush = "AT", Movies = true }, () => Now,
            (commands, _) =>
            {
                sent.AddRange(commands);
                return Task.CompletedTask;
            }, TextWriter.Null);
        promoter.SetListings(Listings());

        var program = await promoter.ShowNextAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Casablanca", program!.Title);
        var expected = Promo.Commands(new PromoBox("\"Casablanca\"", "KTIVDT", "AT"), null);
        Assert.Equal(expected.Select(c => Convert.ToHexString(c.Render())),
            sent.Select(c => Convert.ToHexString(c.Render())));
    }

    [Fact]
    public void Playbook_ReadsTheAutoPromoSettings()
    {
        var playbook = PlaybookLoader.Parse("""
            autoPromo:
              intervalSeconds: 60
              movies: true
              channels: [KTIV*]
              order: Random
              brush: AT
              side: Left
            commands: []
            """);

        var settings = playbook.AutoPromo!;
        Assert.Equal(60, settings.IntervalSeconds);
        Assert.True(settings.Movies);
        Assert.Null(settings.Premium);
        Assert.Equal(PromoOrder.Random, settings.Order);
        Assert.Equal(PromoSide.Left, settings.Side);
        settings.Check();
    }

    [Theory]
    [InlineData(4, 3, 10, null)]
    [InlineData(60, 0.25, 10, null)]
    [InlineData(60, 3, 101, null)]
    [InlineData(60, 3, 10, "ABC")]
    public void Check_RejectsBadSettings(int interval, double within, int repeat, string? brush) =>
        Assert.Throws<ArgumentException>(() => new AutoPromoSettings
        {
            IntervalSeconds = interval, WithinHours = within, Repeat = repeat, Brush = brush
        }.Check());
}
