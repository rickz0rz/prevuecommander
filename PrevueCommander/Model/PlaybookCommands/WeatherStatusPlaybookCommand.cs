using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// Sets the weather status: a digit, a label and a text, for example the name of the city. The digit '0' clears the
/// weather. Send it in a session, after <c>!!address</c>.
/// </summary>
public record WeatherStatusPlaybookCommand : SimplePlaybookCommand
{
    /// <summary>The status digit, '0' to '9'. '0' clears the weather.</summary>
    public char Digit { get; init; } = '1';

    /// <summary>The label, 0 to 10 characters. A weather text finds it with its label pattern.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>The text that ESQ shows above the weather text.</summary>
    public string Text { get; init; } = string.Empty;

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) =>
        [new WeatherStatusCommand(Digit, Label, Text)];
}
