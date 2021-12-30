namespace Prevue.Commands.Model;

public class Channel
{
    public string SourceName { get; set; } = string.Empty;
    public string? ChannelNumber { get; set; }
    public string? CallSign { get; set; }
    public byte? TimeSlotMask { get; set; }
}