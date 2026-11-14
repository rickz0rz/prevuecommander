using System.Globalization;
using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Sets the clock of ESQ to the local time, or to <see cref="Date"/>.</summary>
public record ClockPlaybookCommand : SimplePlaybookCommand
{
    /// <summary>True to send the current time even if <see cref="Date"/> is set.</summary>
    public bool UseCurrentDate { get; init; }

    /// <summary>A local date and time, for example "2020-11-01T16:00".</summary>
    public string? Date { get; init; }

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context)
    {
        var time = UseCurrentDate || string.IsNullOrWhiteSpace(Date)
            ? context.Now
            : DateTime.Parse(Date, CultureInfo.InvariantCulture);
        return [new ClockCommand(time)];
    }

    /// <summary>
    /// A warning for a clock with daylight saving time and no DST window in the same commands, or null. ESQ then shows
    /// the clock one hour early, unless the drive already has the DST window of that year.
    /// </summary>
    public static string? DstWarning(IReadOnlyCollection<BaseCommand> commands)
    {
        var clock = commands.OfType<ClockCommand>().FirstOrDefault(c => c.DaylightSavingTime);
        if (clock == null || commands.OfType<DaylightSavingTimeCommand>().Any())
            return null;
        return $"Warning: !!clock sends a time with daylight saving time ({clock.DateTime:yyyy-MM-dd HH:mm}), but the " +
               "playbook has no !!configureDst. If ESQ has no DST window of this year, it shows the clock one hour " +
               "early. Add !!configureDst to the playbook.";
    }
}
