using System.Text;

namespace PrevueCommander.Commands;

public class ChannelProgramCommand : BaseCommand
{
    private readonly byte _timeSlot;
    private readonly DateTime _dateTime;
    private readonly string _sourceName;
    private readonly bool _isMovie;
    private readonly string _description;
    
    public ChannelProgramCommand(byte timeSlot, DateTime dateTime, string sourceName,
        bool isMovie, string description) : base((byte)'P')
    {
        _timeSlot = timeSlot;
        _dateTime = dateTime;
        _sourceName = sourceName;
        _isMovie = isMovie;
        _description = description;
    }

    protected override byte[] GetMessageBytes()
    {
        var messageBytes = new List<byte>
        {
            _timeSlot,
            Helpers.GetJulianDate(_dateTime)
        };

        messageBytes.AddRange(Encoding.ASCII.GetBytes(_sourceName));
        messageBytes.Add(0x12); // End of source

        messageBytes.Add((byte)(!_isMovie ? 0x1 : 0x3));
        messageBytes.AddRange(Helpers.ConvertStringToBytes(_description, Helpers.GuideFontTokenMapper));
        
        return messageBytes.ToArray();
    }
}