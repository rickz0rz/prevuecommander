namespace Prevue.Commands.Model;

/// <summary>The settings of the 'F' status packet that a playbook can change.</summary>
public record StatusConfiguration
{
    /// <summary>
    /// The time zone, '1' to 'H'. ESQ adds (time zone - 6) hours to the clock and to the times of the grid. With '6',
    /// ESQ shows the times as they are sent, and PrevueCommander sends local times.
    /// </summary>
    public char TimeZone { get; init; } = '6';

    /// <summary>The scroll speed of the grid, '1' to '8'. ESQ uses '4' for other values.</summary>
    public char ScrollSpeed { get; init; } = '3';

    /// <summary>True if the area observes daylight saving time.</summary>
    public bool DaylightSavingTime { get; init; } = true;

    /// <summary>Minutes that ESQ adds to the clock that it shows, 0 to 59.</summary>
    public int ClockOffsetMinutes { get; init; }
}
