using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Shows the current weather on the control line (W0), from the weather status and the weather text.</summary>
public record ShowWeatherPlaybookCommand : SimplePlaybookCommand
{
    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) => [new ShowWeatherCommand()];
}
