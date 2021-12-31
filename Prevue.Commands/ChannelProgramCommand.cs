using System.Text;
using Prevue.Core;

namespace Prevue.Commands;

public class ChannelProgramCommand : BaseCommand
{
    private readonly byte _timeSlot;
    private readonly DateTime _startDateTime;
    private readonly string _sourceName;
    private readonly bool _isMovie;
    private readonly string _description;
    
    public ChannelProgramCommand(DateTime startDateTime, string sourceName,
        bool isMovie, string description) : base((byte)'P')
    {
        _timeSlot = CalculateTimeSlot(startDateTime);
        _startDateTime = startDateTime.AddHours(-1);
        _sourceName = sourceName;
        _isMovie = isMovie;
        _description = description;
    }
    
    private byte CalculateTimeSlot(DateTime dateTime)
    {
        const int uvsgHourOffset = 10; // Their day starts at 5AM, 5 * 2 = 10
        // var ts = ((dateTime.Hour * 2) + 1 - uvsgHourOffset);
        var ts = ((dateTime.Hour * 2) - uvsgHourOffset);

        if (dateTime.Minute >= 15 && dateTime.Minute < 45)
        {
            ts++;
        }
        else if (dateTime.Minute >= 45)
        {
            ts += 2;
        }

        if (ts > 48)
        {
            ts -= 48;
        }
        else if (ts <= 0)
        {
            ts += 48;
        }

        return (byte)ts;
    }

    protected override byte[] GetMessageBytes()
    {
        var messageBytes = new List<byte>
        {
            _timeSlot,
            Helpers.GetJulianDate(_startDateTime)
        };

        messageBytes.AddRange(Encoding.ASCII.GetBytes(_sourceName));
        messageBytes.Add(0x12); // End of source

        messageBytes.Add((byte)(!_isMovie ? 0x1 : 0x3));
        messageBytes.AddRange(Helpers.ConvertStringToBytes(_description, Helpers.GuideFontTokenMapper));
        
        return messageBytes.ToArray();
    }
}