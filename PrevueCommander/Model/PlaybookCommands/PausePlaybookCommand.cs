using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Waits before the next command, for example to keep a promo on the screen before the next promo.</summary>
public record PausePlaybookCommand : SimplePlaybookCommand
{
    public double Seconds { get; init; } = 1;

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) =>
        [new PauseCommand(TimeSpan.FromSeconds(Seconds))];
}
