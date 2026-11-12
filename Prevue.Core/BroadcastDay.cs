namespace Prevue.Core;

/// <summary>
/// The broadcast day of Prevue. A broadcast day starts at 5:00 AM and has 48 half-hour slots. Slot 1 is 5:00 AM to
/// 5:29 AM, and slot 48 is 4:30 AM to 4:59 AM of the next calendar day.
/// </summary>
/// <remarks>
/// ESQ keeps the listings of two broadcast days: the current day (the primary group) and the next day (the secondary
/// group). It changes its current day at 5:30 AM, when the slot becomes 2 (<c>_ESQDISP_DrawStatusBanner_Impl</c>).
/// </remarks>
public static class BroadcastDay
{
    public const int SlotsPerDay = 48;

    public static readonly TimeSpan DayStart = TimeSpan.FromHours(5);

    public static readonly TimeSpan DayChange = new(5, 30, 0);

    private static readonly TimeSpan SlotLength = TimeSpan.FromMinutes(30);

    /// <summary>The broadcast day that ESQ uses as its current day at the given local time.</summary>
    public static DateOnly Current(DateTime time) =>
        DateOnly.FromDateTime(time.TimeOfDay < DayChange ? time.AddDays(-1) : time);

    /// <summary>The broadcast day that contains the given local time.</summary>
    public static DateOnly Containing(DateTime time) => DateOnly.FromDateTime(time - DayStart);

    /// <summary>The local time at which the broadcast day starts.</summary>
    public static DateTime StartOf(DateOnly day) => day.ToDateTime(TimeOnly.MinValue) + DayStart;

    /// <summary>
    /// The group code of a broadcast day: its day of the year, modulo 256. The 'C' and 'P' commands carry it, and ESQ
    /// accepts them only for its primary and secondary groups.
    /// </summary>
    public static byte GroupCode(DateOnly day) => (byte)(day.DayOfYear & 0xFF);

    /// <summary>
    /// The slot (1 to 48) of the broadcast day in which the local time falls. A time before the day gives slot 1, and
    /// a time after the day gives a value above 48.
    /// </summary>
    public static int Slot(DateOnly day, DateTime time)
    {
        var offset = time - StartOf(day);
        return offset <= TimeSpan.Zero ? 1 : (int)(offset / SlotLength) + 1;
    }
}
