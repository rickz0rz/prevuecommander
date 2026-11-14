using Prevue.Commands;
using Xunit;

namespace PrevueCommander.Tests;

public class LocalAdLayoutTests
{
    private static List<AdLine> Lines(string text) => LocalAdLayout.Lines(new LocalAdCommand(1, text).EncodedText);

    [Fact]
    public void AlignmentTokens_StartNewLines_AndColorsAreNotText()
    {
        var lines = Lines("%COLOR%%BLACK%%CYAN%One%CENTER%%COLOR%%BLACK%%YELLOW%Two%RIGHT%Three");

        Assert.Equal(["One", "Two", "Three"], lines.Select(l => l.Text));
        Assert.Equal([AdAlignment.None, AdAlignment.Center, AdAlignment.Right], lines.Select(l => l.Alignment));
        Assert.Empty(LocalAdLayout.Problems(lines));
    }

    [Fact]
    public void AnAlignmentTokenAtTheStart_DoesNotMakeAnEmptyLine()
    {
        var line = Assert.Single(Lines("%LEFT%Text"));
        Assert.Equal(AdAlignment.Left, line.Alignment);
    }

    [Fact]
    public void ALineWithAnAlignmentCode_Has40Characters()
    {
        var lines = Lines("%CENTER%" + new string('a', 40) + "bc");

        Assert.Equal([new string('a', 40), "bc"], lines.Select(l => l.Text));
        Assert.Equal("Line 1 is longer than 40 characters. ESQ continues it on line 2, also in the middle of a word.",
            Assert.Single(LocalAdLayout.Problems(lines)));
    }

    [Fact]
    public void ALineWithoutAnAlignmentCode_Has40Characters()
    {
        // On Esquire, this text showed "...789abcd" on line 1 and "efghij" on line 2.
        var lines = Lines("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghij");

        Assert.Equal(["ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcd", "efghij"], lines.Select(l => l.Text));
        Assert.Equal([AdAlignment.None, AdAlignment.None], lines.Select(l => l.Alignment));
        Assert.Single(LocalAdLayout.Problems(lines));
    }

    [Fact]
    public void TheNextLineOfAFullAlignedLine_HasNoAlignment()
    {
        var lines = Lines("%CENTER%" + new string('a', 40) + "bc");

        Assert.Equal([AdAlignment.Center, AdAlignment.None], lines.Select(l => l.Alignment));
    }

    [Fact]
    public void AFullLine_BeforeAnAlignmentToken_IsNotAProblem()
    {
        var lines = Lines("%LEFT%" + new string('a', 40) + "%CENTER%Next");

        Assert.Equal(2, lines.Count);
        Assert.Empty(LocalAdLayout.Problems(lines));
    }

    [Fact]
    public void MoreThanSixLines_AreDropped()
    {
        var lines = Lines("%LEFT%1%LEFT%2%LEFT%3%LEFT%4%LEFT%5%LEFT%6%LEFT%7%LEFT%8");

        Assert.Equal(8, lines.Count);
        Assert.Equal("The ad has 8 lines. ESQ shows only 6, and drops \"7 8\".",
            Assert.Single(LocalAdLayout.Problems(lines)));
    }

    [Theory]
    [InlineData(AdAlignment.Left, "|ab                                      |")]
    [InlineData(AdAlignment.Center, "|                   ab                   |")]
    [InlineData(AdAlignment.Right, "|                                      ab|")]
    public void Preview_ShowsTheAlignment(AdAlignment alignment, string preview) =>
        Assert.Equal(preview, LocalAdLayout.Preview(new AdLine(alignment, "ab", false)));
}
