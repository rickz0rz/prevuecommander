using System.Text;

namespace Prevue.Commands;

/// <summary>The alignment of a line of a local ad.</summary>
public enum AdAlignment
{
    /// <summary>The line has no alignment code. ESQ shows it at the left.</summary>
    None,
    Left,
    Center,
    Right
}

/// <summary>One line of a local ad, as ESQ shows it.</summary>
/// <param name="Alignment">The alignment code at the start of the line.</param>
/// <param name="Text">The characters of the line.</param>
/// <param name="IsFull">True if the line has no more places. ESQ continues the text on the next line.</param>
public sealed record AdLine(AdAlignment Alignment, string Text, bool IsFull);

/// <summary>
/// Calculates the lines of a local ad as ESQ shows them (<c>_LADFUNC_DrawEntryPreview</c>), to find text that ESQ
/// breaks or drops.
/// </summary>
/// <remarks>
/// <para>
/// A line has 40 characters, with or without an alignment code. A test with Esquire showed this. When a line is full,
/// ESQ continues the text on the next line, also in the middle of a word. The next line has no alignment, so ESQ shows
/// it at the left. An alignment code that is not at the start of a line starts a new line.
/// </para>
/// <para>
/// ESQ shows the number of lines of the LINE field of the 'F' command, a maximum of 6. Prevue Commander sends 6. ESQ
/// drops the text after the last line. ESQ ignores CR and LF. The color code ($03 and two colors) is not text.
/// </para>
/// </remarks>
public static class LocalAdLayout
{
    /// <summary>The characters of a line. The alignment code is not a character.</summary>
    public const int LineCharacters = 40;

    /// <summary>The lines that ESQ shows, from the LINE field of the 'F' command.</summary>
    public const int MaximumLines = 6;

    private const byte ColorCode = 0x03;

    /// <summary>All the lines of the encoded text of an ad. ESQ shows only the first <see cref="MaximumLines"/>.</summary>
    public static List<AdLine> Lines(ReadOnlySpan<byte> text)
    {
        var lines = new List<AdLine>();
        var alignment = AdAlignment.None;
        var characters = new StringBuilder();

        void EndLine(bool isFull)
        {
            lines.Add(new AdLine(alignment, characters.ToString(), isFull));
            alignment = AdAlignment.None;
            characters.Clear();
        }

        for (var i = 0; i < text.Length; i++)
        {
            var value = text[i];
            if (value == ColorCode)
            {
                i += 2;
                continue;
            }

            if (value is (byte)'\r' or (byte)'\n')
                continue;

            var code = Alignment(value);
            if (code != AdAlignment.None)
            {
                // An alignment code that is not at the start of a line starts a new line.
                if (characters.Length > 0)
                    EndLine(false);
                alignment = code;
                continue;
            }

            characters.Append((char)value);
            if (characters.Length == LineCharacters)
                EndLine(true);
        }

        if (characters.Length > 0 || alignment != AdAlignment.None)
            EndLine(false);
        return lines;
    }

    /// <summary>The problems of the lines, as sentences. Empty if ESQ shows all the text as it is written.</summary>
    public static List<string> Problems(IReadOnlyList<AdLine> lines)
    {
        var problems = new List<string>();
        for (var i = 0; i < Math.Min(lines.Count, MaximumLines) - 1; i++)
        {
            // A full line is broken only if the next line continues it, without an alignment code.
            if (lines[i].IsFull && lines[i + 1].Alignment == AdAlignment.None)
                problems.Add($"Line {i + 1} is longer than {lines[i].Text.Length} characters. " +
                             $"ESQ continues it on line {i + 2}, also in the middle of a word.");
        }

        if (lines.Count > MaximumLines)
        {
            var dropped = string.Join(" ", lines.Skip(MaximumLines).Select(l => l.Text.Trim()));
            problems.Add($"The ad has {lines.Count} lines. ESQ shows only {MaximumLines}, and drops \"{dropped}\".");
        }

        return problems;
    }

    /// <summary>A line as text for a preview, with its alignment in the 40 characters of a line, between two '|'.</summary>
    public static string Preview(AdLine line)
    {
        var width = LineCharacters;
        var text = line.Text.Length > width ? line.Text : line.Alignment switch
        {
            AdAlignment.Center => line.Text.PadLeft((width + line.Text.Length) / 2).PadRight(width),
            AdAlignment.Right => line.Text.PadLeft(width),
            _ => line.Text.PadRight(width)
        };
        return $"|{text}|";
    }

    private static AdAlignment Alignment(byte value) => value switch
    {
        0x18 => AdAlignment.Center,
        0x19 => AdAlignment.Left,
        0x1A => AdAlignment.Right,
        _ => AdAlignment.None
    };
}
