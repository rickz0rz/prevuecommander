using System.Globalization;
using System.Text;

namespace Prevue.Core;

/// <summary>Converts text to the bytes that ESQ can show.</summary>
public static class PrevueText
{
    /// <summary>
    /// Makes the text printable in the Prevue fonts. Letters lose their accents, typographic quotation marks and
    /// dashes become ASCII, and other characters outside printable ASCII are removed. The result has a maximum of
    /// <paramref name="maximumLength"/> characters.
    /// </summary>
    public static string ToPrintableAscii(string text, int maximumLength = int.MaxValue)
    {
        var cleaned = Clean(text).Trim();
        return cleaned.Length > maximumLength ? cleaned[..maximumLength].TrimEnd() : cleaned;
    }

    private static string Clean(string text)
    {
        var result = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            var replacement = c switch
            {
                >= ' ' and < '\x7F' => c,
                '‘' or '’' => '\'',
                '“' or '”' => '"',
                '–' or '—' => '-',
                '\t' or '\n' or '\r' or ' ' => ' ',
                _ => (char?)null
            };
            if (replacement != null)
                result.Append(replacement.Value);
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts text with tokens to bytes. A token is a name between two '%' characters, for example "%CENTER%", and
    /// <paramref name="tokenMapper"/> gives its bytes. "%%" is a literal '%'. The other characters become printable
    /// ASCII, as in <see cref="ToPrintableAscii"/>, without a trim.
    /// </summary>
    /// <exception cref="FormatException">A token is unknown or has no closing '%'.</exception>
    public static byte[] Encode(string text, Func<string, byte[]?> tokenMapper)
    {
        var bytes = new List<byte>(text.Length);
        var position = 0;
        while (position < text.Length)
        {
            var start = text.IndexOf('%', position);
            if (start < 0)
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(Clean(text[position..])));
                break;
            }

            bytes.AddRange(Encoding.ASCII.GetBytes(Clean(text[position..start])));
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
