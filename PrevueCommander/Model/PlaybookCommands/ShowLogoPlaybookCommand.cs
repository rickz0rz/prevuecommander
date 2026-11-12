using Prevue.Commands;
using Prevue.Commands.Control;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Shows the loaded logo of LOGO.LST in the top half of the screen. ESQ then loads the next logo.</summary>
public record ShowLogoPlaybookCommand : SimplePlaybookCommand
{
    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) => [new ShowLogoCommand()];
}
