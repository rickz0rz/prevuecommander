using Prevue.Core;

namespace Prevue.Commands;

/// <summary>
/// The 'L' command: a local text ad. ESQ keeps the ads when the local text ad source of the 'f' configuration is 'R' or
/// 'S'. ESQ shows the next ad in the top half of the screen when the control line sends a type 12 packet.
/// </summary>
/// <remarks>
/// The text can have these tokens (see <see cref="MapToken"/>):
/// <list type="bullet">
/// <item>%COLOR% and then two color tokens: the background and the foreground of the text that follows.</item>
/// <item>%LEFT%, %CENTER% and %RIGHT%: the alignment of a line. A token that is not at the start of a line starts a
/// new line.</item>
/// </list>
/// An ad has a maximum of 6 lines of 40 characters. ESQ does not wrap words. See <see cref="LocalAdLayout"/>.
/// </remarks>
public class LocalAdCommand : BaseCommand
{
    /// <summary>ESQ keeps a maximum of 45 ads, with the numbers 1 to 45.</summary>
    public const int MaximumAds = 45;

    /// <summary>ESQ counts a line error for a record that is longer than 304 bytes.</summary>
    private const int MaximumRecordLength = 0x130;

    private readonly byte[] _text;

    /// <param name="characters">The characters that the text can have. ESQ 9.0.14 shows Latin-1 characters.</param>
    public LocalAdCommand(int number, string text, CharacterSet characters = CharacterSet.Latin1) : base('L')
    {
        if (number is < 1 or > MaximumAds)
            throw new ArgumentOutOfRangeException(nameof(number), number, $"An ad number is 1 to {MaximumAds}.");

        _text = PrevueText.Encode(text, MapToken, characters);
        if (_text.Length + 1 > MaximumRecordLength)
            throw new ArgumentException(
                $"Ad {number} has {_text.Length} bytes. The maximum is {MaximumRecordLength - 1}.", nameof(text));

        Number = number;
        Text = text;
    }

    public int Number { get; }

    public string Text { get; }

    /// <summary>The bytes of the text, with the tokens as their codes.</summary>
    public ReadOnlySpan<byte> EncodedText => _text;

    public override string ToString() => $"{nameof(LocalAdCommand)}: #{Number} \"{Text}\"";

    protected override byte[] GetPayload() => [(byte)Number, .. _text];

    /// <summary>
    /// The bytes of an ad token. The colors are pen numbers of the palette of ESQ. The names are those of the default
    /// palette.
    /// </summary>
    public static byte[]? MapToken(string token) => token switch
    {
        "COLOR" => [0x03],
        "TRANSPARENT" => [(byte)'0'],
        "WHITE" => [(byte)'1'],
        "BLACK" => [(byte)'2'],
        "YELLOW" => [(byte)'3'],
        "RED" => [(byte)'4'],
        "CYAN" => [(byte)'5'],
        "GRAY" or "GREY" => [(byte)'6'],
        "BLUE" => [(byte)'7'],
        "CENTER" => [0x18],
        "LEFT" => [0x19],
        "RIGHT" => [0x1A],
        _ => null
    };
}
