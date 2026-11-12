using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Selects the units that accept the next commands. "*" selects all units.</summary>
public record AddressPlaybookCommand : SimplePlaybookCommand
{
    public string Target { get; init; } = "*";

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) => [new AddressCommand(Target)];
}
