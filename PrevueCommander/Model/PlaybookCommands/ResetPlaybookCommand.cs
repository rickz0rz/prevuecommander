using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// Reboots the Amiga. ESQ accepts the reset only directly after an address command, so this command sends both.
/// </summary>
public record ResetPlaybookCommand : SimplePlaybookCommand
{
    public string Target { get; init; } = "*";

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) =>
        [new AddressCommand(Target), new ResetCommand()];
}
