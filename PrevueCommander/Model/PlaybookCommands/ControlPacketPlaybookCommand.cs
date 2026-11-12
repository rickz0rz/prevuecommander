using Prevue.Commands;
using Prevue.Commands.Control;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// Sends one packet on the control line, for the types that have no playbook command. In a double-quoted YAML text,
/// <c>\x12</c> is the separator of the right box and the left box.
/// </summary>
public record ControlPacketPlaybookCommand : SimplePlaybookCommand
{
    public int Type { get; init; }
    public string Body { get; init; } = string.Empty;

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) =>
        [new RawControlCommand(Type, Body)];
}
