using Prevue.Commands;
using Prevue.Commands.Control;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Removes the promo or the logo from the top half of the screen. The genlock video shows there.</summary>
public record ClearTopHalfPlaybookCommand : SimplePlaybookCommand
{
    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) => [new ClearTopHalfCommand()];
}
