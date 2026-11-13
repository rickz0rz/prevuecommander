using System.Globalization;
using System.Text;

namespace Prevue.Core;

/// <summary>The characters that the text of a command can have.</summary>
public enum CharacterSet
{
    /// <summary>
    /// Printable ASCII and the Latin-1 characters from $A1 to $FF, for example "é", "ñ" and "©". The fonts of ESQ 9.0.14
    /// have these characters.
    /// </summary>
    Latin1,

    /// <summary>Printable ASCII only. Letters lose their accents. Use it for older versions of ESQ.</summary>
    Ascii
}

/// <summary>Converts text to the bytes that ESQ can show.</summary>
public static class PrevueText
{
    /// <summary>
    /// Makes the text printable in the Prevue fonts, as ASCII. Letters lose their accents, typographic quotation
    /// marks and dashes become ASCII, and other characters outside printable ASCII are removed. The result has a
    /// maximum of <paramref name="maximumLength"/> characters.
    /// </summary>
    public static string ToPrintableAscii(string text, int maximumLength = int.MaxValue) =>
        ToPrintable(text, CharacterSet.Ascii, maximumLength);

    /// <summary>
    /// Makes the text printable in the Prevue fonts. With <see cref="CharacterSet.Latin1"/>, the Latin-1 characters
    /// stay, for example "é". The other characters change as in <see cref="ToPrintableAscii"/>. The result has a
    /// maximum of <paramref name="maximumLength"/> characters. Each character of the result is one byte for ESQ (see
    /// <see cref="GetBytes"/>).
    /// </summary>
    public static string ToPrintable(string text, CharacterSet characters, int maximumLength = int.MaxValue)
    {
        var cleaned = Clean(text, characters).Trim();
        return cleaned.Length > maximumLength ? cleaned[..maximumLength].TrimEnd() : cleaned;
    }

    /// <summary>The bytes of a text from <see cref="ToPrintable"/>: one Latin-1 byte for each character.</summary>
    public static byte[] GetBytes(string printableText) => Encoding.Latin1.GetBytes(printableText);

    private static string Clean(string text, CharacterSet characters)
    {
        var result = new StringBuilder(text.Length);
        // The composed form makes one character of a letter and its accent, for example "e" and U+0301 become "é".
        foreach (var c in text.Normalize(NormalizationForm.FormC))
        {
            if (characters == CharacterSet.Latin1 && c is >= '\xA1' and <= '\xFF' && c != '\xAD')
            {
                result.Append(c);
                continue;
            }

            var replacement = c switch
            {
                >= ' ' and < '\x7F' => c,
                '‘' or '’' => '\'',
                '“' or '”' => '"',
                '–' or '—' => '-',
                '\t' or '\n' or '\r' or '\xA0' => ' ',
                _ => (char?)null
            };
            if (replacement != null)
            {
                result.Append(replacement.Value);
                continue;
            }

            // A letter with an accent becomes the letter without the accent, for example "é" becomes "e".
            foreach (var part in c.ToString().Normalize(NormalizationForm.FormD))
                if (part is >= ' ' and < '\x7F')
                    result.Append(part);
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts text with tokens to bytes. A token is a name between two '%' characters, for example "%CENTER%", and
    /// <paramref name="tokenMapper"/> gives its bytes. "%%" is a literal '%'. The other characters become printable
    /// characters, as in <see cref="ToPrintable"/>, without a trim.
    /// </summary>
    /// <exception cref="FormatException">A token is unknown or has no closing '%'.</exception>
    public static byte[] Encode(string text, Func<string, byte[]?> tokenMapper,
        CharacterSet characters = CharacterSet.Latin1)
    {
        var bytes = new List<byte>(text.Length);
        var position = 0;
        while (position < text.Length)
        {
            var start = text.IndexOf('%', position);
            if (start < 0)
            {
                bytes.AddRange(GetBytes(Clean(text[position..], characters)));
                break;
            }

            bytes.AddRange(GetBytes(Clean(text[position..start], characters)));
            var end = text.IndexOf('%', start + 1);
            if (end < 0)
                throw new FormatException($"The token at position {start} of \"{text}\" has no closing '%'.");

            var token = text[(start + 1)..end];
            if (token.Length == 0)
                bytes.Add((byte)'%');
            else
                bytes.AddRange(tokenMapper(token.ToUpperInvariant())
                               ?? throw new FormatException($"The token %{token}% in \"{text}\" is unknown."));
            position = end + 1;
        }

        return bytes.ToArray();
    }
}
