using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// Shows the weather on the control line: the current weather (W0) from the weather status and the weather text, or
/// the 3-day forecast (W3) of <c>!!weatherForecast</c>.
/// </summary>
public record ShowWeatherPlaybookCommand : SimplePlaybookCommand
{
    /// <summary>True shows the 3-day forecast (W3). False shows the current weather (W0).</summary>
    public bool Forecast { get; init; }

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context) => [new ShowWeatherCommand(Forecast)];
}
