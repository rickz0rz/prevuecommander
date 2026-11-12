namespace Prevue.Commands;

/// <summary>
/// The 'K' command: the date and the time of the clock of ESQ. ESQ applies it only when the last field of the 'f'
/// configuration is '2'. ESQ adds (time zone - 6) hours when it shows the clock, so with the time zone '6' of the
/// 'F' command the clock shows the time as sent.
/// </summary>
public class ClockCommand(DateTime dateTime) : BaseCommand('K')
{
    public DateTime DateTime { get; } = dateTime;

    public override string ToString() => $"{nameof(ClockCommand)}: {DateTime:yyyy-MM-dd HH:mm:ss}";

    protected override byte[] GetPayload() =>
    [
        (byte)DateTime.DayOfWeek,
        (byte)(DateTime.Month - 1),
        (byte)(DateTime.Day - 1),
        (byte)(DateTime.Year - 1900),
        (byte)DateTime.Hour,
        (byte)DateTime.Minute,
        (byte)DateTime.Second,
        (byte)(DateTime.IsDaylightSavingTime() ? 1 : 0)
    ];
}
