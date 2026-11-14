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

    protected override byte[] GetPayload() => [(byte)Digit, .. Ascii(Label), Separator, .. WeatherText.Bytes(Text)];
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
            text.AddRange([.. WeatherText.Bytes(line), LineBreak]);

        return [(byte)Minutes, (byte)Color, (byte)Icon, Unused, .. Ascii(LabelPattern), Separator, .. text];
    }
}

/// <summary>One day of the 3-day forecast: the picture, the high and the low.</summary>
/// <param name="Date">The day. ESQ finds the day by its day of the year.</param>
/// <param name="Icon">The weather picture: 2 to 6 for the brushes PWI1 to PWI5 of the drive. Null shows no picture.</param>
/// <param name="High">The high temperature, -99 to 999. Null shows "???".</param>
/// <param name="Low">The low temperature, -99 to 999. Null shows "???".</param>
public sealed record WeatherForecastDay(DateOnly Date, int? Icon, int? High, int? Low);

/// <summary>
/// The 'w' command: the 3-day forecast. ESQ shows it when the control line sends <c>W3</c>, until the minutes end
/// (<c>_UNKNOWN_ParseListAndUpdateEntries</c>, <c>_WDISP_DrawWeatherStatusSummary</c>).
/// </summary>
/// <remarks>
/// <para>
/// The payload is the label pattern, $12, the minutes, and then each day: '+', the day of the year (3 digits), the
/// icon (1 character), the high (3 characters) and the low (3 characters). '?' in a field means "no value".
/// </para>
/// <para>
/// ESQ keeps the days from tomorrow to 4 days from now and draws the first 3. It ignores a day that is not in this
/// range. A day without data shows the text "No Forecast Weather Data Available". ESQ compares the label pattern with the label
/// of the 'i' command. Without an 'i' command the label is empty, so only the pattern "*" matches. ESQ shows the
/// panels only when the digit of the 'I' command is not '0'.
/// </para>
/// </remarks>
public class WeatherForecastCommand : BaseCommand
{
    /// <summary>The most days that ESQ keeps.</summary>
    public const int MaximumDays = 4;

    private const byte Separator = 0x12;

    /// <param name="minutes">The minutes that ESQ shows the forecast, 1 to 255.</param>
    /// <param name="labelPattern">The pattern of the label of the 'i' command, 1 to 10 characters.</param>
    /// <param name="days">The days, a maximum of 4.</param>
    public WeatherForecastCommand(int minutes, string labelPattern, IReadOnlyList<WeatherForecastDay> days) : base('w')
    {
        if (minutes is < 1 or > 255)
            throw new ArgumentOutOfRangeException(nameof(minutes), minutes, "The minutes must be 1 to 255.");
        if (labelPattern.Length == 0)
            throw new ArgumentException("The label pattern must have 1 or more characters. Use \"*\" for all labels.",
                nameof(labelPattern));
        WeatherText.CheckLabel(labelPattern, nameof(labelPattern));
        if (days.Count is 0 or > MaximumDays)
            throw new ArgumentException($"A forecast has 1 to {MaximumDays} days.", nameof(days));
        foreach (var day in days)
        {
            if (day.Icon is < 2 or > 6)
                throw new ArgumentOutOfRangeException(nameof(days), day.Icon,
                    "The icon of a forecast day must be 2 to 6, or empty for no picture.");
            if (day.High is < -99 or > 999 || day.Low is < -99 or > 999)
                throw new ArgumentOutOfRangeException(nameof(days), $"{day.High}/{day.Low}",
                    "A temperature must be -99 to 999.");
        }

        Minutes = minutes;
        LabelPattern = labelPattern;
        Days = days.ToList();
    }

    public int Minutes { get; }

    public string LabelPattern { get; }

    public IReadOnlyList<WeatherForecastDay> Days { get; }

    /// <summary>The day of the year that ESQ uses for a date: 1 for January 1.</summary>
    public static int DayKey(DateOnly date) => date.DayOfYear;

    public override string ToString() =>
        $"{nameof(WeatherForecastCommand)}: {Minutes} min, [{LabelPattern}] " +
        string.Join(", ", Days.Select(d => $"{d.Date:yyyy-MM-dd} {d.Icon?.ToString() ?? "-"} {d.High}/{d.Low}"));

    protected override byte[] GetPayload()
    {
        var text = new List<byte>();
        foreach (var day in Days)
        {
            var fields = $"+{DayKey(day.Date):D3}{(day.Icon is { } icon ? (char)('0' + icon) : '?')}" +
                         $"{Temperature(day.High)}{Temperature(day.Low)}";
            text.AddRange(Ascii(fields));
        }

        return [.. Ascii(LabelPattern), Separator, (byte)Minutes, .. text];
    }

    private static string Temperature(int? value) => value switch
    {
        null => "???",
        < 0 => $"-{-value:D2}",
        _ => $"{value:D3}"
    };
}

/// <summary>
/// Type 1 with <c>W0</c>: shows the current weather, from the 'I' and 'W' commands. With <c>W3</c> it shows the
/// 3-day forecast of the 'w' command.
/// </summary>
public sealed class ShowWeatherCommand(bool forecast = false) : ControlCommand(1)
{
    public bool Forecast { get; } = forecast;

    protected override string Body => Forecast ? "W3" : "W0";
}

/// <summary>The rules for the text of the weather commands.</summary>
internal static class WeatherText
{
    public static void CheckLabel(string label, string name)
    {
        if (label.Length > WeatherStatusCommand.MaximumLabelLength)
            throw new ArgumentException(
                $"The label \"{label}\" has more than {WeatherStatusCommand.MaximumLabelLength} characters.", name);
        // ESQ compares the labels byte by byte, so a label has only printable ASCII characters.
        if (label.Any(c => c is < ' ' or > '~'))
            throw new ArgumentException($"The label \"{label}\" must have printable ASCII characters only.", name);
    }

    /// <summary>
    /// Checks a text. It can have printable ASCII characters and the Latin-1 characters from $A1 to $FF, for example
    /// "°" and "é". The fonts of ESQ 9.0.4 have these characters.
    /// </summary>
    public static void CheckText(string text, string name)
    {
        if (text.Any(c => c is < ' ' or > '\xFF' || c is > '~' and < '\xA1' || c == '\xAD'))
            throw new ArgumentException(
                $"The weather text \"{text}\" must have printable ASCII or Latin-1 characters only.", name);
    }

    /// <summary>The Latin-1 bytes of a checked text.</summary>
    public static byte[] Bytes(string text) => System.Text.Encoding.Latin1.GetBytes(text);
}
