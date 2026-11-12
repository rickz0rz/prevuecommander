using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Tells ESQ to save the received data to its drive at the next box off.</summary>
public record SavePlaybookCommand : SimplePlaybookCommand
{
    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) => [new SaveCommand()];
}
