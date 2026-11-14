using Prevue.Commands.Control;
using Prevue.Commands.Listings;
using PrevueCommander.Model.PlaybookCommands;

namespace PrevueCommander.Model;

public enum PromoOrder
{
    /// <summary>The next program that fits.</summary>
    Soonest,

    /// <summary>A program that fits, by chance.</summary>
    Random
}

/// <summary>
/// The automatic promos: after the commands, Prevue Commander sends a promo at each interval. It chooses the program
/// from the listings that it sent. The promos need the control line.
/// </summary>
public record AutoPromoSettings
{
    /// <summary>The seconds between two promos, 5 or more.</summary>
    public int IntervalSeconds { get; init; } = 120;

    /// <summary>The hours after the current half hour for the start of the program, 0.5 to 24.</summary>
    public double WithinHours { get; init; } = 3;

    /// <summary>True to also choose a program that starts in the current half hour.</summary>
    public bool IncludeNow { get; init; }

    /// <summary>True for movies only, false for no movies. No value chooses from all the programs.</summary>
    public bool? Movies { get; init; }

    /// <summary>True for premium channels only, false for no premium channels. No value chooses from all.</summary>
    public bool? Premium { get; init; }

    /// <summary>Call letters (with '*' for any text) or channel numbers. Empty for all channels.</summary>
    public List<string> Channels { get; init; } = [];

    /// <summary>Parts of titles. Upper case and lower case are the same. Empty for all titles.</summary>
    public List<string> Titles { get; init; } = [];

    public PromoOrder Order { get; init; } = PromoOrder.Soonest;

    /// <summary>The number of the last promos whose titles do not show again, 0 to 100.</summary>
    public int Repeat { get; init; } = 10;

    /// <summary>The ID of the background in BRUSH.INI, for example "AT". No value keeps the background.</summary>
    public string? Brush { get; init; }

    /// <summary>The box of the promo.</summary>
    public PromoSide Side { get; init; } = PromoSide.Right;

    /// <summary><c>Box</c> shows the background of the box. <c>Text</c> draws only the text.</summary>
    public PromoStyle Style { get; init; } = PromoStyle.Box;

    /// <summary>Makes sure that the settings are correct.</summary>
    /// <exception cref="ArgumentException">A setting is not correct.</exception>
    public void Check()
    {
        if (IntervalSeconds < 5)
            throw new ArgumentException("autoPromo: intervalSeconds must be 5 or more.");
        if (WithinHours is < 0.5 or > 24)
            throw new ArgumentException("autoPromo: withinHours must be 0.5 to 24.");
        if (Repeat is < 0 or > 100)
            throw new ArgumentException("autoPromo: repeat must be 0 to 100.");
        if (Brush is { Length: not 2 })
            throw new ArgumentException("autoPromo: a brush is an ID of 2 characters, for example AT.");
    }

    public AutoPromoCriteria Criteria() =>
        new(Movies, Premium, Channels, Titles, WithinHours, IncludeNow, Repeat, Order == PromoOrder.Random);
}
