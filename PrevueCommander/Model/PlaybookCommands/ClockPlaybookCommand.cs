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
}
