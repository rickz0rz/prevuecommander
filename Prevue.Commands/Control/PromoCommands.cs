using Prevue.Core;

namespace Prevue.Commands.Control;

/// <summary>
/// One box of a promo. ESQ searches its listings for a program whose title contains <see cref="Title"/>, on a channel
/// whose call letters match <see cref="Channels"/>. Upper case and lower case are the same. Put the title in quotes
/// to find the exact title only.
/// </summary>
/// <param name="Title">The title to find.</param>
/// <param name="Channels">A pattern for the call letters, for example <c>*</c> or <c>KTIV*</c>.</param>
/// <param name="Brush">The ID of the background in <c>BRUSH.INI</c>, for example <c>AT</c>, or null to keep it.</param>
public sealed record PromoBox(string Title, string Channels = "*", string? Brush = null);

/// <summary>Type 2: the backgrounds of the right box and the left box. <c>00</c> keeps a background.</summary>
public sealed class PromoBrushesCommand : ControlCommand
{
    public const string KeepBrush = "00";

    public PromoBrushesCommand(string? right, string? left) : base(2)
    {
        Right = Validate(right ?? KeepBrush);
        Left = Validate(left ?? KeepBrush);
    }

    public string Right { get; }
    public string Left { get; }

    protected override string Body => Right + Left;

    private static string Validate(string brush) => brush.Length == 2 && brush.All(char.IsAsciiLetterOrDigit)
        ? brush
        : throw new ArgumentException($"A brush is an ID of 2 letters or digits from BRUSH.INI, for example AT: {brush}");
}

/// <summary>Type 4: the box that ESQ tries first when a promo has two boxes.</summary>
public sealed class PromoOrderCommand(bool rightFirst) : ControlCommand(4)
{
    public bool RightFirst { get; } = rightFirst;

    protected override string Body => RightFirst ? "L" : "R";
}

/// <summary>Type 17: the titles to find for the right box and the left box. ESQ clears them after each promo.</summary>
public sealed class PromoTitlesCommand(string? right, string? left) : ControlCommand(17)
{
    protected override string Body => (right ?? string.Empty) + (left == null ? string.Empty : Separator + left);
}

/// <summary>How ESQ shows a promo.</summary>
public enum PromoStyle
{
    /// <summary>Type 1 with <c>1</c>: the text of the promo on the background of its box.</summary>
    Box,

    /// <summary>
    /// Type 1 with <c>8</c>: the same search, but ESQ draws only the text, without the background of the box. The text
    /// goes over what the top half shows: the genlock video, or the colors of the last logo.
    /// </summary>
    Text
}

/// <summary>
/// Type 1 with <c>1</c> or <c>8</c>: shows the promo. ESQ tries the boxes in the order of
/// <see cref="PromoOrderCommand"/>, and shows the first box whose title it finds. If it finds no title, it shows the
/// loaded logo.
/// </summary>
public sealed class ShowPromoCommand(string? rightChannels, string? leftChannels, PromoStyle style = PromoStyle.Box)
    : ControlCommand(1)
{
    public PromoStyle Style { get; } = style;

    // ESQ needs a pattern before the separator, also for a promo with only a left box.
    protected override string Body =>
        (Style == PromoStyle.Box ? "1" : "8") + (rightChannels ?? "*") +
        (leftChannels == null ? string.Empty : Separator + leftChannels);
}

/// <summary>
/// Type 1 with <c>5</c>: moves the top of the grid up or down. The configuration byte 49 (LrbnBannerControl) must be
/// 'Y'. Without it, ESQ shows the loaded logo. A clear of the top half moves the grid back.
/// </summary>
/// <remarks>
/// The body is <c>5</c>, the position as two hex digits, the time as seconds and tenths of a second, and an optional
/// pattern of call letters. In a test with Esquire, position $A0 was lower than $80.
/// </remarks>
public sealed class MoveGridCommand : ControlCommand
{
    public MoveGridCommand(byte position, TimeSpan duration, string? channels = null) : base(1)
    {
        var tenths = (int)Math.Round(duration.TotalSeconds * 10);
        if (tenths is < 0 or > 99)
            throw new ArgumentOutOfRangeException(nameof(duration), "The time of a grid move is from 0 to 9.9 seconds.");
        if (channels != null && (channels.Length == 0 || channels.Any(c => c is <= ' ' or > '~')))
            throw new ArgumentException("The channel pattern must be printable ASCII text without spaces.",
                nameof(channels));
        Position = position;
        Tenths = tenths;
        Channels = channels;
    }

    public byte Position { get; }
    public int Tenths { get; }
    public string? Channels { get; }

    protected override string Body => $"5{Position:X2}{Tenths / 10}{Tenths % 10}{Channels}";
}

/// <summary>Type 1 with <c>3</c>: removes the promo or the logo. The top half shows the genlock video.</summary>
public sealed class ClearTopHalfCommand() : ControlCommand(1)
{
    protected override string Body => "3";
}

/// <summary>Type 1 with <c>D</c>: shows the loaded logo in the top half. ESQ then loads the next logo of LOGO.LST.</summary>
public sealed class ShowLogoCommand() : ControlCommand(1)
{
    protected override string Body => "D";
}

public static class Promo
{
    /// <summary>
    /// Makes the packets of a promo, in the order of the Esquire launcher: the brushes (only if a box has a brush),
    /// the order (only for two boxes), the titles, and the show command.
    /// </summary>
    public static List<ControlCommand> Commands(PromoBox? right, PromoBox? left, bool rightFirst = true,
        PromoStyle style = PromoStyle.Box)
    {
        if (right == null && left == null)
            throw new ArgumentException("A promo needs a right box, a left box, or both.");

        right = Clean(right);
        left = Clean(left);
        var commands = new List<ControlCommand>();
        if (right?.Brush != null || left?.Brush != null)
            commands.Add(new PromoBrushesCommand(right?.Brush, left?.Brush));
        if (right != null && left != null)
            commands.Add(new PromoOrderCommand(rightFirst));
        commands.Add(new PromoTitlesCommand(right?.Title, left?.Title));
        commands.Add(new ShowPromoCommand(right?.Channels, left?.Channels, style));
        return commands;
    }

    /// <summary>
    /// Converts the title and the pattern to printable ASCII, as the listings builder converts the program text. Then a
    /// title with typographic quotes or accents finds the program.
    /// </summary>
    private static PromoBox? Clean(PromoBox? box)
    {
        if (box == null)
            return null;
        var title = PrevueText.ToPrintableAscii(box.Title, ControlCommand.MaximumBodyLength);
        var channels = PrevueText.ToPrintableAscii(box.Channels, ControlCommand.MaximumBodyLength);
        if (title.Length == 0 || channels.Length == 0)
            throw new ArgumentException("Each box of a promo needs a title and a channel pattern.");
        return box with { Title = title, Channels = channels };
    }
}
