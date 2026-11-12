using System.Globalization;
using Prevue.Commands;
using Prevue.Commands.Control;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// Moves the top of the grid up or down with the control line. Send <c>lrbnBannerControl: 'Y'</c> in
/// <c>!!newLookConfiguration</c> first. <c>!!clearTopHalf</c> moves the grid back.
/// </summary>
public record MoveGridPlaybookCommand : SimplePlaybookCommand
{
    /// <summary>The position as two hex digits, from 00 to FF. A larger value is lower on the screen.</summary>
    public string Position { get; init; } = "A0";

    /// <summary>The time of the move, from 0 to 9.9 seconds.</summary>
    public double Seconds { get; init; } = 1;

    /// <summary>An optional pattern of call letters, for example <c>KTIV*</c>.</summary>
    public string? Channels { get; init; }

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context)
    {
        if (Position.Length != 2 ||
            !byte.TryParse(Position, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var position))
            throw new ArgumentException($"The grid position must be two hex digits, for example A0: \"{Position}\".");
        return [new MoveGridCommand(position, TimeSpan.FromSeconds(Seconds), Channels)];
    }
}
