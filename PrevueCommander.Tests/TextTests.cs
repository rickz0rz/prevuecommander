using Prevue.Commands;
using Prevue.Core;
using Xunit;

namespace PrevueCommander.Tests;

public class TextTests
{
    [Fact]
    public void ToPrintableAscii_ReplacesTypographicCharactersAndAccents()
    {
        Assert.Equal("Don't - \"Cafe\"", PrevueText.ToPrintableAscii("Don’t — “Café”☃"));
        Assert.Equal("abc", PrevueText.ToPrintableAscii("abcdef", 3));
    }

    [Fact]
    public void Encode_KeepsSpacesAroundTokens()
    {
        Assert.Equal("A \u0018 B"u8.ToArray(), PrevueText.Encode("A %CENTER% B", LocalAdCommand.MapToken));
    }

    [Theory]
    [InlineData("100%")]
    [InlineData("%NOPE%")]
    public void Encode_ReportsBadTokens(string text)
    {
        Assert.Throws<FormatException>(() => PrevueText.Encode(text, LocalAdCommand.MapToken));
    }
}
