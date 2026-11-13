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
    public void ToPrintable_KeepsLatin1Characters()
    {
        Assert.Equal("90 Day Fiancé", PrevueText.ToPrintable("90 Day Fiancé", CharacterSet.Latin1));
        Assert.Equal("Niño © 1993 - 'Über'", PrevueText.ToPrintable("Niño © 1993 – ‘Über’☃", CharacterSet.Latin1));
    }

    [Fact]
    public void ToPrintable_ComposesALetterAndItsAccent()
    {
        Assert.Equal("Fiancé", PrevueText.ToPrintable("Fiance\u0301", CharacterSet.Latin1));
        Assert.Equal("Fiance", PrevueText.ToPrintable("Fiance\u0301", CharacterSet.Ascii));
    }

    [Fact]
    public void ToPrintable_RemovesTheAccentsOfLettersOutsideLatin1()
    {
        Assert.Equal("Erdos", PrevueText.ToPrintable("Erdős", CharacterSet.Latin1));
        Assert.Equal("A B", PrevueText.ToPrintable("A\u00A0B\u00AD", CharacterSet.Latin1));
    }

    [Fact]
    public void GetBytes_GivesOneLatin1BytePerCharacter()
    {
        Assert.Equal([(byte)'F', 0xE9], PrevueText.GetBytes("Fé"));
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

    [Fact]
    public void Encode_UsesTheCharacterSet()
    {
        Assert.Equal([(byte)'C', (byte)'a', (byte)'f', 0xE9], PrevueText.Encode("Café", LocalAdCommand.MapToken));
        Assert.Equal("Cafe"u8.ToArray(), PrevueText.Encode("Café", LocalAdCommand.MapToken, CharacterSet.Ascii));
    }
}
