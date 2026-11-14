using System.Globalization;
using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>One day of <see cref="WeatherForecastPlaybookCommand"/>.</summary>
public record WeatherForecastDaySettings
{
    /// <summary>The day, for example "2020-11-02". Without a date, the first day is tomorrow, the next day the day after.</summary>
    public string? Date { get; init; }

    /// <summary>The weather picture: 2 to 6 for the brushes PWI1 to PWI5 of the drive. Empty shows no picture.</summary>
    public int? Icon { get; init; }

    /// <summary>The high temperature. Empty shows "???".</summary>
    public int? High { get; init; }

    /// <summary>The low temperature. Empty shows "???".</summary>
    public int? Low { get; init; }
}

/// <summary>
/// Sends the 3-day forecast: a picture, a high and a low for each day, from tomorrow. <c>!!showWeather</c> with
/// <c>forecast: true</c> shows it. Send it in a session, after <c>!!weatherStatus</c> with a digit that is not '0'.
/// </summary>
public record WeatherForecastPlaybookCommand : SimplePlaybookCommand
{
    /// <summary>The minutes that ESQ shows the forecast, 1 to 255.</summary>
    public int Minutes { get; init; } = 60;

    /// <summary>The label pattern. Keep "*": ESQ has no other label unless a forecast label was set.</summary>
    public string Label { get; init; } = "*";

    /// <summary>The days, a maximum of 4. ESQ shows the first 3.</summary>
    public List<WeatherForecastDaySettings> Days { get; init; } = [];

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context)
    {
        var tomorrow = DateOnly.FromDateTime(context.Now).AddDays(1);
        var days = Days.Select((day, index) => new WeatherForecastDay(
            string.IsNullOrWhiteSpace(day.Date)
                ? tomorrow.AddDays(index)
                : DateOnly.Parse(day.Date, CultureInfo.InvariantCulture),
            day.Icon, day.High, day.Low)).ToList();
        return [new WeatherForecastCommand(Minutes, Label, days)];
    }
}
