using Prevue.Commands.Control;

namespace Prevue.Commands;

/// <summary>
/// The 'I' command: the weather status. It sets a status digit, a label and a text, for example the name of the city.
/// ESQ shows the text above the weather text of the 'W' command when the control line sends <c>W0</c>
/// (<c>_ESQPROTO_ParseDigitLabelAndDisplay</c>). ESQ accepts it only after an 'A' command, and it saves it in
/// curday.dat.
/// </summary>
/// <remarks>
/// The payload is the digit, the label, $12 and the text. ESQ shows the weather only when the digit is not '0', so
/// the digit '0' clears the weather. ESQ counts a line error for a payload that is longer than
/// <see cref="MaximumPayloadLength"/> bytes, and ignores the command.
/// </remarks>
public class WeatherStatusCommand : BaseCommand
{
    /// <summary>The longest label. ESQ loses the characters after it.</summary>
    public const int MaximumLabelLength = 10;

    /// <summary>The longest payload that ESQ accepts, with the NUL terminator.</summary>
    public const int MaximumPayloadLength = 39;

    private const byte Separator = 0x12;

    public WeatherStatusCommand(char digit, string label, string text) : base('I')
    {
        if (digit is < '0' or > '9')
            throw new ArgumentOutOfRangeException(nameof(digit), digit, "The weather status digit must be '0' to '9'.");
        WeatherText.CheckLabel(label, nameof(label));
        WeatherText.CheckText(text, nameof(text));

        Digit = digit;
        Label = label;
        Text = text;
        var length = GetPayload().Length + 1;
        if (length > MaximumPayloadLength)
            throw new ArgumentException(
                $"The weather status has {length} bytes. ESQ accepts a maximum of {MaximumPayloadLength}: " +
                $"the digit, the label, a separator, the text and the end. Make the text shorter.", nameof(text));
    }

    public char Digit { get; }

    public string Label { get; }

    public string Text { get; }

    public override string ToString() => $"{nameof(WeatherStatusCommand)}: {Digit} [{Label}] \"{Text}\"";

    protected override byte[] GetPayload() => [(byte)Digit, .. Ascii(Label), Separator, .. Ascii(Text)];
}

/// <summary>
/// The 'W' command: a weather text, for example a forecast or an alert. ESQ keeps it only if its label pattern matches
/// the label of the last 'I' command (<c>_UNKNOWN_ParseRecordAndUpdateDisplay</c>). ESQ shows it when the control line
/// sends <c>W0</c>, until the minutes end.
/// </summary>
/// <remarks>
/// The payload is the minutes, the color, the icon, a byte that ESQ ignores, the label pattern, $12 and the lines. Each
/// line ends with $18: a test in Esquire showed that ESQ drops the text after the last $18. A NUL ends the record, so the first four bytes cannot be 0. ESQ takes 1 from the minutes
/// each minute. The label pattern can have '*' and '?', and upper case and lower case are different.
/// </remarks>
public class WeatherTextCommand : BaseCommand
{
    /// <summary>The most lines that ESQ keeps.</summary>
    public const int MaximumLines = 10;

    private const byte Separator = 0x12;
    private const byte LineBreak = 0x18;
    private const byte Unused = (byte)'0';

    /// <param name="minutes">The minutes that ESQ shows the text, 1 to 255.</param>
    /// <param name="color">The color byte, 1 to 255. ESQ shows it only on the diagnostics screen.</param>
    /// <param name="icon">The weather picture: 2 to 6 for the brushes PWI1 to PWI5 of the drive, 1 for none.</param>
    /// <param name="labelPattern">The pattern of the label of the 'I' command, 1 to 10 characters.</param>
    /// <param name="lines">The lines of the text, a maximum of 10.</param>
    public WeatherTextCommand(int minutes, int color, int icon, string labelPattern, IReadOnlyList<string> lines)
        : base('W')
    {
        if (minutes is < 1 or > 255)
            throw new ArgumentOutOfRangeException(nameof(minutes), minutes, "The minutes must be 1 to 255.");
        if (color is < 1 or > 255)
            throw new ArgumentOutOfRangeException(nameof(color), color, "The color must be 1 to 255.");
        if (icon is < 1 or > 6)
            throw new ArgumentOutOfRangeException(nameof(icon), icon, "The icon must be 1 (none) to 6.");
        if (labelPattern.Length == 0)
            throw new ArgumentException("The label pattern must have 1 or more characters. ESQ ignores an empty pattern.",
                nameof(labelPattern));
        WeatherText.CheckLabel(labelPattern, nameof(labelPattern));
        if (lines.Count is 0 or > MaximumLines)
            throw new ArgumentException($"A weather text has 1 to {MaximumLines} lines.", nameof(lines));
        foreach (var line in lines)
            WeatherText.CheckText(line, nameof(lines));

        Minutes = minutes;
        Color = color;
        Icon = icon;
        LabelPattern = labelPattern;
        Lines = lines.ToList();
    }

    public int Minutes { get; }

    public int Color { get; }

    public int Icon { get; }

    public string LabelPattern { get; }

    public IReadOnlyList<string> Lines { get; }

    public override string ToString() =>
        $"{nameof(WeatherTextCommand)}: {Minutes} min, icon {Icon}, [{LabelPattern}] \"{string.Join(" / ", Lines)}\"";

    protected override byte[] GetPayload()
    {
        // ESQ drops the text after the last line break, so each line ends with one.
        var text = new List<byte>();
        foreach (var line in Lines)
            text.AddRange([.. Ascii(line), LineBreak]);

        return [(byte)Minutes, (byte)Color, (byte)Icon, Unused, .. Ascii(LabelPattern), Separator, .. text];
    }
}

/// <summary>Type 1 with <c>W0</c>: shows the current weather, from the 'I' and 'W' commands.</summary>
public sealed class ShowWeatherCommand() : ControlCommand(1)
{
    protected override string Body => "W0";
}

/// <summary>The rules for the text of the weather commands.</summary>
internal static class WeatherText
{
    public static void CheckLabel(string label, string name)
    {
        if (label.Length > WeatherStatusCommand.MaximumLabelLength)
            throw new ArgumentException(
                $"The label \"{label}\" has more than {WeatherStatusCommand.MaximumLabelLength} characters.", name);
        CheckText(label, name);
    }

    public static void CheckText(string text, string name)
    {
        if (text.Any(c => c is < ' ' or > '~'))
            throw new ArgumentException($"The weather text \"{text}\" must have printable ASCII characters only.", name);
    }
}
