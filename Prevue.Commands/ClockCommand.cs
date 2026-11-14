namespace Prevue.Commands;

/// <summary>
/// The 'K' command: the date and the time of the clock of ESQ. ESQ applies it only when the last field of the 'f'
/// configuration is '2'. ESQ adds (time zone - 6) hours when it shows the clock, so with the time zone '6' of the
/// 'F' command the clock shows the time as sent.
/// </summary>
/// <remarks>
/// The last byte tells ESQ that the time includes daylight saving time. ESQ compares it with its own DST window of
/// the 'g' commands. Tests in Esquire showed that ESQ moves the clock one hour when they do not agree. A summer time
/// with the flag and without a DST window of that year shows one hour early. A summer time without the flag, in the
/// window, shows one hour late. ESQ counts a line error and keeps its clock if the clock setting of the 'f'
/// configuration is not '2'.
/// </remarks>
public class ClockCommand(DateTime dateTime) : BaseCommand('K')
{
    public DateTime DateTime { get; } = dateTime;

    /// <summary>True if the time includes daylight saving time. The last byte of the payload.</summary>
    public bool DaylightSavingTime => DateTime.IsDaylightSavingTime();

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
        (byte)(DaylightSavingTime ? 1 : 0)
    ];
}
