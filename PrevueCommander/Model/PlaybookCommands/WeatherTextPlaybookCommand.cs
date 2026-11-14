using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// Sends a weather text, for example a forecast or an alert. ESQ keeps it only if <see cref="Label"/> matches the label
/// of the weather status. Send it in a session, after <c>!!weatherStatus</c>.
/// </summary>
public record WeatherTextPlaybookCommand : SimplePlaybookCommand
{
    /// <summary>The minutes that ESQ shows the text, 1 to 255.</summary>
    public int Minutes { get; init; } = 60;

    /// <summary>The weather picture: 2 to 6 for the brushes PWI1 to PWI5 of the drive, 1 for none.</summary>
    public int Icon { get; init; } = 1;

    /// <summary>The color byte, 1 to 255. ESQ shows it only on the diagnostics screen.</summary>
    public int Color { get; init; } = 1;

    /// <summary>The pattern of the label of the weather status. '*' and '?' are wildcards.</summary>
    public string Label { get; init; } = "*";

    /// <summary>The lines of the text, a maximum of 10.</summary>
    public List<string> Lines { get; init; } = [];

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) =>
        [new WeatherTextCommand(Minutes, Color, Icon, Label, Lines)];
}
