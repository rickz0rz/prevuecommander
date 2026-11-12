using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Ends the session. ESQ saves its data first if a save command came before.</summary>
public record BoxOffPlaybookCommand : SimplePlaybookCommand
{
    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) => [new BoxOffCommand()];
}
