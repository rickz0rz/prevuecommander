using Prevue.Commands;
using Prevue.Commands.Model;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Sends the status packet ('F'). ESQ needs it before a channel lineup or a reset of the local ads.</summary>
public record ConfigurationPlaybookCommand : SimplePlaybookCommand
{
    public StatusConfiguration Configuration { get; init; } = new();

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) =>
        [new ConfigurationCommand(Configuration)];
}
