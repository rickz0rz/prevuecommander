namespace PrevueCommander.WeatherGov;

/// <summary>The current weather of a station. The values use SI units, as weather.gov sends them.</summary>
/// <param name="FeelsLikeCelsius">The wind chill or the heat index, if weather.gov has one.</param>
/// <param name="PressurePascals">The barometric pressure, in pascals.</param>
/// <param name="VisibilityMeters">The visibility, in meters.</param>
/// <param name="Icon">The ESQ picture, 2 to 6, or null.</param>
public sealed record WeatherCurrent(
    DateTimeOffset? Time,
    string? Description,
    double? TemperatureCelsius,
    double? FeelsLikeCelsius,
    double? Humidity,
    double? WindDirectionDegrees,
    double? WindSpeedKmh,
    int? Icon,
    double? DewPointCelsius = null,
    double? PressurePascals = null,
    double? VisibilityMeters = null);

/// <summary>One day of the forecast. The temperatures are in degrees Fahrenheit.</summary>
/// <param name="High">The temperature of the day period.</param>
/// <param name="Low">The temperature of the night period after the day.</param>
public sealed record WeatherDay(DateOnly Date, int? Icon, int? High, int? Low, string? ShortForecast);

/// <summary>The weather of a location: the current weather, the next forecast period and the next days.</summary>
public sealed record WeatherReport(
    string? City,
    string? State,
    WeatherCurrent? Current,
    WeatherGovPeriod? Next,
    IReadOnlyList<WeatherDay> Days)
{
    /// <summary>Reads the forecast and the current weather of a location.</summary>
    /// <param name="today">The local date. The days start tomorrow.</param>
    public static async Task<WeatherReport> ReadAsync(WeatherGovClient client, double latitude, double longitude,
        DateOnly today, bool current, CancellationToken cancellationToken = default)
    {
        var point = await client.GetPointAsync(latitude, longitude, cancellationToken);
        var periods = await client.GetForecastAsync(point, cancellationToken);
        var observation = current
            ? await client.GetLatestObservationAsync(point, cancellationToken: cancellationToken)
            : null;
        return Build(point, periods, observation, today);
    }

    public static WeatherReport Build(WeatherGovPoint point, IReadOnlyList<WeatherGovPeriod> periods,
        WeatherGovObservation? observation, DateOnly today, int days = 3)
    {
        var list = new List<WeatherDay>();
        for (var i = 1; i <= days; i++)
        {
            var date = today.AddDays(i);
            // The time of a period is the local time of the forecast office.
            var day = periods.FirstOrDefault(p => p.IsDaytime && DateOnly.FromDateTime(p.StartTime.DateTime) == date);
            var night = periods.FirstOrDefault(p => !p.IsDaytime && DateOnly.FromDateTime(p.StartTime.DateTime) == date);
            if (day == null && night == null)
                continue;
            var main = day ?? night!;
            list.Add(new WeatherDay(date, WeatherIcons.Choose(main.Icon, main.ShortForecast), day?.Temperature,
                night?.Temperature, main.ShortForecast));
        }

        WeatherCurrent? now = null;
        if (observation != null)
        {
            now = new WeatherCurrent(observation.Timestamp, observation.TextDescription,
                observation.Temperature?.Value, observation.WindChill?.Value ?? observation.HeatIndex?.Value,
                observation.RelativeHumidity?.Value, observation.WindDirection?.Value, observation.WindSpeed?.Value,
                WeatherIcons.Choose(observation.Icon, observation.TextDescription), observation.DewPoint?.Value,
                observation.BarometricPressure?.Value, observation.Visibility?.Value);
        }

        var city = point.RelativeLocation?.Properties;
        return new WeatherReport(city?.City, city?.State, now, periods.FirstOrDefault(), list);
    }
}
