using Prevue.Commands.Model;
using Prevue.Core;

namespace Prevue.Commands;

/// <summary>
/// The 'C' command: the channel lineup of a broadcast day. ESQ accepts it only after an 'F' command, and only for its
/// current or next broadcast day. If the lineup is new, ESQ deletes the programs of the day, so the 'P' commands of
/// all the channels must follow.
/// </summary>
/// <remarks>
/// The payload is the group code of the day, and then for each channel: $12, the source attribute, the source name,
/// $11 and the channel number, $14 and the mask of the 48 slots (6 bytes), and $01 and the call letters.
/// </remarks>
public class ChannelLineUpCommand : BaseCommand
{
    /// <summary>ESQ saves a maximum of 200 channels.</summary>
    public const int MaximumChannels = 200;

    private const byte SourceStart = 0x12;
    private const byte ChannelNumberStart = 0x11;
    private const byte SlotMaskStart = 0x14;
    private const byte CallLettersStart = 0x01;

    public ChannelLineUpCommand(DateOnly day, IEnumerable<Channel> channels) : base('C')
    {
        Day = day;
        Channels = channels.ToList();
        if (Channels.Count > MaximumChannels)
            throw new ArgumentException($"A lineup has a maximum of {MaximumChannels} channels.", nameof(channels));
    }

    public DateOnly Day { get; }

    public IReadOnlyList<Channel> Channels { get; }

    public override string ToString() =>
        $"{nameof(ChannelLineUpCommand)}: {Day:yyyy-MM-dd}, {Channels.Count} channel(s): " +
        string.Join(", ", Channels.Select(c => $"[{c.SourceName}] {c.CallSign} {c.ChannelNumber}"));

    protected override byte[] GetPayload()
    {
        var bytes = new List<byte> { BroadcastDay.GroupCode(Day) };
        foreach (var channel in Channels)
        {
            bytes.Add(SourceStart);
            // Bit 0 is always set. Bit 1 makes the row red (a premium channel). Bit 7 lets ESQ change "In Stereo" to
            // the stereo symbol (_ESQSHARED_NormalizeInStereoTag).
            bytes.Add((byte)(0x01 | (channel.Premium ? 0x02 : 0) | (channel.Stereo ? 0x80 : 0)));
            bytes.AddRange(Ascii(ValidSourceName(channel.SourceName)));
            bytes.Add(ChannelNumberStart);
            bytes.AddRange(Ascii(Limit(channel.ChannelNumber, Channel.MaximumChannelNumberLength)));
            bytes.Add(SlotMaskStart);
            bytes.AddRange([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]); // The channel has listings in all 48 slots.
            bytes.Add(CallLettersStart);
            bytes.AddRange(Ascii(Limit(channel.CallSign, Channel.MaximumCallSignLength)));
        }

        return bytes.ToArray();
    }

    private static string Limit(string? text, int length) =>
        PrevueText.ToPrintableAscii(text ?? string.Empty, length);

    internal static string ValidSourceName(string sourceName)
    {
        if (sourceName.Length is 0 or > Channel.MaximumSourceNameLength || !sourceName.All(char.IsAsciiLetterOrDigit))
            throw new ArgumentException(
                $"The source name \"{sourceName}\" must have 1 to {Channel.MaximumSourceNameLength} letters or digits.");
        return sourceName;
    }
}
