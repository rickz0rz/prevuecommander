namespace Prevue.Commands;

/// <summary>
/// The 'g2' and 'g3' commands: the daylight saving time window. 'g3' sets the primary window, which ESQ always uses.
/// 'g2' sets the secondary window. The original data files have the same window for both.
/// </summary>
/// <remarks>
/// The payload is the length as two ASCII digits, then $04 and the start, then $13 and the end. The length counts
/// the bytes after the digits and the NUL. A time is <c>YYYYDDDHH:MM</c>, where DDD is the day of the year, starting
/// at 0. The start is the local time just after clocks go forward, and the end is the local time just after clocks
/// go back. ESQ accepts a year only within one year of its clock (<c>_DATETIME_ParseString</c>).
/// <para>
/// ESQ keeps the window in memory. It writes the window to dst.dat only after a '%' save command and the next box off
/// (<c>_ESQPARS_PersistStateDataAfterCommand</c>, <c>_DATETIME_SavePairToFile</c>), and it reads dst.dat when it starts
/// (<c>_DST_LoadBannerPairFromFiles</c>). A test in Esquire showed both. The 'K' clock command uses the window: see
/// <see cref="ClockCommand"/>.
/// </para>
/// </remarks>
public class DaylightSavingTimeCommand : BaseCommand
{
    private const byte StartMarker = 0x04;
    private const byte EndMarker = 0x13;

    public DaylightSavingTimeCommand(char window, DateTime start, DateTime end) : base((byte)'g', (byte)window)
    {
        if (window is not ('2' or '3'))
            throw new ArgumentOutOfRangeException(nameof(window), window, "The window must be '2' or '3'.");
        Window = window;
        Start = start;
        End = end;
    }

    public char Window { get; }

    public DateTime Start { get; }

    public DateTime End { get; }

    public override string ToString() =>
        $"{nameof(DaylightSavingTimeCommand)}: g{Window} {Start:yyyy-MM-dd HH:mm} to {End:yyyy-MM-dd HH:mm}";

    protected override byte[] GetPayload()
    {
        byte[] window = [StartMarker, .. Ascii(Format(Start)), EndMarker, .. Ascii(Format(End))];
        return [.. Ascii($"{window.Length + 1:D2}"), .. window];
    }

    private static string Format(DateTime time) => $"{time.Year:D4}{time.DayOfYear - 1:D3}{time:HH:mm}";

    /// <summary>
    /// The local times just after the two changes of the offset of the time zone in the year, or null if the zone
    /// does not change its offset in that year.
    /// </summary>
    public static (DateTime Start, DateTime End)? FindWindow(int year, TimeZoneInfo zone)
    {
        DateTime? start = null, end = null;
        var step = TimeSpan.FromMinutes(15);
        var time = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);
        var offset = zone.GetUtcOffset(time);
        for (; time.Year <= year; time += step)
        {
            var next = zone.GetUtcOffset(time);
            if (next != offset)
            {
                var local = DateTime.SpecifyKind(time + next, DateTimeKind.Unspecified);
                if (local.Year == year)
                {
                    if (next > offset)
                        start ??= local;
                    else
                        end ??= local;
                }

                offset = next;
            }
        }

        return start is { } s && end is { } e ? (s, e) : null;
    }
}
