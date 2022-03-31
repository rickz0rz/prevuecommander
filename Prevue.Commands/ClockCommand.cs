namespace Prevue.Commands;

public class ClockCommand : BaseCommand
{
    private readonly DateTime _dateTime;

    public ClockCommand(DateTime dateTime) : base('K')
    {
        // Convert the datetime from eastern to central
        // _dateTime = dateTime.AddHours(-1);

        var cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        _dateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime.ToUniversalTime(), cstZone);
    }

    public override string ToString()
    {
        return $"{nameof(ClockCommand)}: Date/time is {_dateTime:O}";
    }

    protected override byte[] GetMessageBytes()
    {
        return new []
        {
            (byte)_dateTime.DayOfWeek,
            (byte)(_dateTime.Month - 1),
            (byte)(_dateTime.Day - 1),
            (byte)(_dateTime.Year - 1900),
            (byte)_dateTime.Hour,
            (byte)_dateTime.Minute,
            (byte)_dateTime.Second,
            (byte)(_dateTime.IsDaylightSavingTime() ? 0x1 : 0x0)
        };
    }
}
