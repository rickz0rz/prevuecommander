using System.Text;
using Prevue.Commands.Model;
using Prevue.Core;

namespace Prevue.Commands;

public class ChannelLineUpCommand : BaseCommand
{
    private readonly IEnumerable<Channel> _channels;
    private readonly DateTime _dateTime;

    public ChannelLineUpCommand(DateTime dateTime, IEnumerable<Channel> channels) : base('C')
    {
        _dateTime = dateTime;
        _channels = channels;
    }

    public override string ToString()
    {
        return
            $"{nameof(ChannelLineUpCommand)}: ({GetMessageBytes().Length} bytes) {_channels.Count()} channel(s) -> {string.Join(", ", _channels.Select(c => $"[{c.SourceName}] {c.CallSign} / {c.ChannelNumber}"))}";
    }

    protected override byte[] GetMessageBytes()
    {
        var messageBytes = new List<byte>();

        messageBytes.Add(Helpers.GetJulianDate(_dateTime));

        foreach (var channel in _channels)
        {
            messageBytes.Add(0x12); // Sequence start
            messageBytes.Add(0x01); // Source attribute

            messageBytes.AddRange(Encoding.ASCII.GetBytes(channel.SourceName));

            if (!string.IsNullOrWhiteSpace(channel.ChannelNumber))
            {
                messageBytes.Add(0x11);
                messageBytes.AddRange(Helpers.ConvertStringToBytes(channel.ChannelNumber,
                    Helpers.GuideFontTokenMapper));
            }

            // TODO: Add channel line-up time slots.
            // messageBytes.Add(0x14); // Time slot to follow, optional?
            // messageBytes.AddRange(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });

            if (!string.IsNullOrWhiteSpace(channel.CallSign))
            {
                messageBytes.Add(0x01);
                messageBytes.AddRange(Helpers.ConvertStringToBytes(channel.CallSign, Helpers.GuideFontTokenMapper));
            }
        }

        return messageBytes.ToArray();
    }
}