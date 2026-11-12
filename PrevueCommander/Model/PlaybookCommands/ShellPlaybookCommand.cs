using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// Runs an AmigaDOS command line on the Amiga, for example "Delete DF0:OLD.IFF", with the 'x' command. ESQ runs it
/// with dos Execute and waits until it ends. There is no output and no result.
/// </summary>
public record ShellPlaybookCommand : SimplePlaybookCommand
{
    public string CommandLine { get; init; } = string.Empty;

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) =>
        [UtilityCommand.Shell(CommandLine)];
}
