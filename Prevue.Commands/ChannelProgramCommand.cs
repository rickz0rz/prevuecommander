using Prevue.Core;

namespace Prevue.Commands;

/// <summary>
/// The 'P' command: the text of the program that starts in a slot of a broadcast day, on the channel with the source
/// name.
/// </summary>
/// <remarks>
/// ESQ changes some text to symbols of its font: "(CC)" or a trailing "|" to the closed caption symbol, and ratings
/// in parentheses, for example "(PG-13)" or "(TV-14)", to rating symbols. For a movie, the grid shows the text up to
/// the second quotation mark on the first line.
/// </remarks>
public class ChannelProgramCommand : BaseCommand
{
    /// <summary>The longest text that the grid can show.</summary>
    public const int MaximumTextLength = 120;

    private const byte SourceEnd = 0x12;

    /// <param name="characters">The characters that the text can have. ESQ 9.0.4 shows Latin-1 characters.</param>
    public ChannelProgramCommand(DateOnly day, int slot, string sourceName, bool isMovie, string text,
        CharacterSet characters = CharacterSet.Latin1) : base('P')
    {
        if (slot is < 1 or > BroadcastDay.SlotsPerDay)
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "A slot is 1 to 48.");

        Day = day;
        Slot = slot;
        SourceName = ChannelLineUpCommand.ValidSourceName(sourceName);
        IsMovie = isMovie;
        Text = PrevueText.ToPrintable(text, characters, MaximumTextLength);
    }

    public DateOnly Day { get; }

    public int Slot { get; }

    public string SourceName { get; }

    public bool IsMovie { get; }

    public string Text { get; }

    public override string ToString() =>
        $"{nameof(ChannelProgramCommand)}: {SourceName} {BroadcastDay.StartOf(Day).AddMinutes((Slot - 1) * 30):yyyy-MM-dd HH:mm} {Text}";

    protected override byte[] GetPayload() =>
    [
        (byte)Slot,
        BroadcastDay.GroupCode(Day),
        .. Ascii(SourceName),
        SourceEnd,
        // Bit 0 is always set. Bit 1 marks a movie.
        (byte)(IsMovie ? 0x03 : 0x01),
        .. PrevueText.GetBytes(Text)
    ];
}
