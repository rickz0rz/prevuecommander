using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// Sends the daylight saving time window of the year to ESQ, from the time zone of this computer.
/// </summary>
public record ConfigureDstPlaybookCommand : SimplePlaybookCommand
{
    /// <summary>The year of the window. The default is the year of the run.</summary>
    public int? Year { get; init; }

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context)
    {
        var year = Year ?? context.Now.Year;
        if (DaylightSavingTimeCommand.FindWindow(year, TimeZoneInfo.Local) is not var (start, end))
        {
            context.Log.WriteLine($"The local time zone has no daylight saving time in {year}. No DST window is sent.");
            return [];
        }

        return [new DaylightSavingTimeCommand('2', start, end), new DaylightSavingTimeCommand('3', start, end)];
    }
}
