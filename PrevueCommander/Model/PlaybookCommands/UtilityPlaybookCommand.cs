using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Tells ESQ to reload files or to show a screen. See <see cref="UtilityAction"/>.</summary>
public record UtilityPlaybookCommand : SimplePlaybookCommand
{
    public UtilityAction Action { get; init; } = UtilityAction.ReloadAdsAndLogos;

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) => [new UtilityCommand(Action)];
}
