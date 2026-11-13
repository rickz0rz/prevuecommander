using Prevue.Commands;
using Prevue.Commands.Control;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>One box of a promo in a playbook. See <see cref="PromoBox"/>.</summary>
public record PromoBoxSettings
{
    public string Title { get; init; } = string.Empty;
    public string Channels { get; init; } = "*";
    public string? Brush { get; init; }

    public PromoBox ToBox() => new(Title, Channels, Brush);
}

public enum PromoSide
{
    Right,
    Left
}

/// <summary>
/// Shows a promo on the control line: a box in the top half of the screen with the call letters, the title, the next
/// time and the channel of a program. ESQ finds the program in its listings, so send the listings first.
/// </summary>
public record PromoPlaybookCommand : SimplePlaybookCommand
{
    public PromoBoxSettings? Right { get; init; }
    public PromoBoxSettings? Left { get; init; }

    /// <summary>The box that ESQ tries first when the promo has two boxes.</summary>
    public PromoSide First { get; init; } = PromoSide.Right;

    /// <summary><c>Box</c> shows the background of the box. <c>Text</c> draws only the text.</summary>
    public PromoStyle Style { get; init; } = PromoStyle.Box;

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) =>
        Promo.Commands(Right?.ToBox(), Left?.ToBox(), First == PromoSide.Right, Style, context.Characters);
}
