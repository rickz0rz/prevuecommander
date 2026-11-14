using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrevueCommander.WeatherGov;

/// <summary>A value with a unit of the weather.gov API, for example a temperature in degrees Celsius.</summary>
public sealed record WeatherGovValue(
    [property: JsonPropertyName("unitCode")] string? UnitCode,
    [property: JsonPropertyName("value")] double? Value);

/// <summary>The city of a point.</summary>
public sealed record WeatherGovCity(
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("state")] string? State);

public sealed record WeatherGovRelativeLocation(
    [property: JsonPropertyName("properties")] WeatherGovCity? Properties);

/// <summary>The forecast office and the links of a point (GET /points/{latitude},{longitude}).</summary>
public sealed record WeatherGovPoint(
    [property: JsonPropertyName("forecast")] string? Forecast,
    [property: JsonPropertyName("observationStations")] string? ObservationStations,
    [property: JsonPropertyName("relativeLocation")] WeatherGovRelativeLocation? RelativeLocation,
    [property: JsonPropertyName("timeZone")] string? TimeZone);

/// <summary>
/// A period of the forecast: a day or a night. The temperature is in degrees Fahrenheit. <see cref="Icon"/> is a link
/// that has the condition codes, for example ".../icons/land/day/rain,40/bkn?size=medium".
/// </summary>
public sealed record WeatherGovPeriod(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("startTime")] DateTimeOffset StartTime,
    [property: JsonPropertyName("isDaytime")] bool IsDaytime,
    [property: JsonPropertyName("temperature")] int? Temperature,
    [property: JsonPropertyName("windSpeed")] string? WindSpeed,
    [property: JsonPropertyName("windDirection")] string? WindDirection,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("shortForecast")] string? ShortForecast);

/// <summary>The latest observation of a weather station. The values use SI units.</summary>
public sealed record WeatherGovObservation(
    [property: JsonPropertyName("timestamp")] DateTimeOffset? Timestamp,
    [property: JsonPropertyName("textDescription")] string? TextDescription,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("temperature")] WeatherGovValue? Temperature,
    [property: JsonPropertyName("relativeHumidity")] WeatherGovValue? RelativeHumidity,
    [property: JsonPropertyName("windDirection")] WeatherGovValue? WindDirection,
    [property: JsonPropertyName("windSpeed")] WeatherGovValue? WindSpeed,
    [property: JsonPropertyName("windChill")] WeatherGovValue? WindChill,
    [property: JsonPropertyName("heatIndex")] WeatherGovValue? HeatIndex);

internal sealed record Feature<T>([property: JsonPropertyName("properties")] T Properties);

internal sealed record FeatureCollection<T>([property: JsonPropertyName("features")] Feature<T>[]? Features);

internal sealed record ForecastProperties([property: JsonPropertyName("periods")] WeatherGovPeriod[]? Periods);

internal sealed record StationProperties(
    [property: JsonPropertyName("stationIdentifier")] string StationIdentifier);

/// <summary>
/// Reads the weather of the National Weather Service from https://api.weather.gov. The API is free and needs no key.
/// It asks each program to identify itself in the User-Agent header.
/// </summary>
public sealed class WeatherGovClient : IDisposable
{
    public const string DefaultAddress = "https://api.weather.gov/";

    private readonly HttpClient _http;

    /// <param name="contact">An optional contact, for example an e-mail address, that the User-Agent header sends.</param>
    /// <param name="handler">The HTTP handler. Tests use a fake handler.</param>
    public WeatherGovClient(string? contact = null, HttpMessageHandler? handler = null, string address = DefaultAddress)
    {
        _http = handler == null ? new HttpClient() : new HttpClient(handler);
        _http.BaseAddress = new Uri(address);
        _http.Timeout = TimeSpan.FromSeconds(30);
        var product = "PrevueCommander (https://github.com/rickz0rz/prevuecommander" +
                      (string.IsNullOrWhiteSpace(contact) ? ")" : $", {contact.Trim()})");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", product);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/geo+json"));
    }

    /// <summary>GET /points/{latitude},{longitude}: the links of the forecast and the stations.</summary>
    public async Task<WeatherGovPoint> GetPointAsync(double latitude, double longitude,
        CancellationToken cancellationToken = default)
    {
        var path = string.Create(CultureInfo.InvariantCulture, $"points/{latitude:0.####},{longitude:0.####}");
        var point = await GetAsync<Feature<WeatherGovPoint>>(path, cancellationToken);
        if (point.Properties.Forecast == null)
            throw new InvalidDataException(
                $"weather.gov has no forecast for {latitude}, {longitude}. weather.gov has data only for the USA.");
        return point.Properties;
    }

    /// <summary>The periods of the forecast of a point: about 7 days and 7 nights.</summary>
    public async Task<WeatherGovPeriod[]> GetForecastAsync(WeatherGovPoint point,
        CancellationToken cancellationToken = default) =>
        (await GetAsync<Feature<ForecastProperties>>(point.Forecast!, cancellationToken)).Properties.Periods ?? [];

    /// <summary>
    /// The latest observation of the nearest station that has a temperature. Null if no station of the first
    /// <paramref name="stations"/> has one.
    /// </summary>
    public async Task<WeatherGovObservation?> GetLatestObservationAsync(WeatherGovPoint point, int stations = 3,
        CancellationToken cancellationToken = default)
    {
        if (point.ObservationStations == null)
            return null;
        var list = await GetAsync<FeatureCollection<StationProperties>>(point.ObservationStations, cancellationToken);
        WeatherGovObservation? first = null;
        foreach (var station in (list.Features ?? []).Take(stations))
        {
            try
            {
                var observation = (await GetAsync<Feature<WeatherGovObservation>>(
                    $"stations/{station.Properties.StationIdentifier}/observations/latest", cancellationToken)).Properties;
                first ??= observation;
                if (observation.Temperature?.Value != null)
                    return observation;
            }
            catch (HttpRequestException)
            {
                // A station without a recent observation gives 404. Try the next station.
            }
        }

        return first;
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"weather.gov answered {(int)response.StatusCode} {response.ReasonPhrase} for {response.RequestMessage?.RequestUri}.",
                null, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken)
               ?? throw new InvalidDataException($"weather.gov sent an empty answer for {path}.");
    }

    public void Dispose() => _http.Dispose();
}
