using System.Globalization;
using System.Text.RegularExpressions;
using Prevue.Commands;
using Prevue.Core;
using PrevueCommander.WeatherGov;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// Reads the weather of a ZIP code or a location from weather.gov, and sends it as the weather of ESQ: the weather
/// status, the current weather as a weather text, and the 3-day forecast. Send it in a session, after
/// <c>!!address</c>. <c>!!showWeather</c> then shows it. weather.gov has data only for the USA.
/// </summary>
public partial record WeatherGovPlaybookCommand : IPlaybookCommand
{
    /// <summary>
    /// The default lines of the current weather, as the "current conditions" of Prevue Weather showed them. ESQ shows
    /// the lines in pairs, at the left and the right of the picture.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultCurrentLines =
    [
        "{condition}", "Temp {temperature}{degree}{unit}",
        "Wind {wind}", "Pressure {pressure}",
        "Humidity {humidity}%", "DewPoint {dewPoint}{degree}{unit}",
        "Visibility {visibility}", ""
    ];

    private static readonly AsyncLocal<Func<string?, WeatherGovClient>?> TestClientFactory = new();

    /// <summary>Makes the weather.gov client. A test sets a fake client for its own async flow only.</summary>
    internal static Func<string?, WeatherGovClient> ClientFactory
    {
        get => TestClientFactory.Value ?? (contact => new WeatherGovClient(contact));
        set => TestClientFactory.Value = value;
    }

    /// <summary>The ZIP code, for example "48201". Use it, or <see cref="Latitude"/> and <see cref="Longitude"/>.</summary>
    public string? Zip { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    /// <summary>The label of the weather status, 0 to 10 characters. The weather text uses it as its pattern.</summary>
    public string Label { get; init; } = "LOCAL";

    /// <summary>The status text at the top of the weather. The same placeholders as <see cref="CurrentLines"/>.</summary>
    public string Status { get; init; } = "{city}";

    /// <summary>True sends the current weather as the weather text, for <c>!!showWeather {}</c> (W0).</summary>
    public bool Current { get; init; } = true;

    /// <summary>True sends the 3-day forecast, for <c>!!showWeather</c> with <c>forecast: true</c> (W3).</summary>
    public bool Forecast { get; init; } = true;

    /// <summary>The minutes that ESQ shows the weather, 1 to 255. A new read starts the minutes again.</summary>
    public int Minutes { get; init; } = 90;

    /// <summary>"F" for degrees Fahrenheit and miles per hour, "C" for degrees Celsius and kilometers per hour.</summary>
    public string Units { get; init; } = "F";

    /// <summary>
    /// The lines of the current weather, a maximum of 10. The placeholders are {city}, {state}, {condition},
    /// {temperature}, {feelsLike}, {dewPoint}, {humidity}, {wind}, {pressure}, {visibility}, {degree}, {unit}, {time},
    /// {next}, {nextForecast}, {nextTemperature} and {nextHighLow}. A missing value shows "--".
    /// </summary>
    public List<string>? CurrentLines { get; init; }

    /// <summary>
    /// A contact, for example an e-mail address. weather.gov asks for it in the User-Agent header, so that it can
    /// contact the owner of a program that causes problems. Without it, the header has only the name of the program.
    /// </summary>
    public string? Contact { get; init; }

    public async Task<IReadOnlyList<BaseCommand>> TransformAsync(PlaybookContext context,
        CancellationToken cancellationToken)
    {
        var celsius = Units.Trim().ToUpperInvariant() switch
        {
            "F" => false,
            "C" => true,
            _ => throw new ArgumentException($"The units of !!weatherGov must be \"F\" or \"C\", not \"{Units}\".")
        };
        if (!Current && !Forecast)
            throw new ArgumentException("!!weatherGov sends nothing: set current, forecast or both to true.");
        var (latitude, longitude) = Location();

        using var client = ClientFactory(Contact);
        var report = await WeatherReport.ReadAsync(client, latitude, longitude, DateOnly.FromDateTime(context.Now),
            Current, cancellationToken);
        context.Log.WriteLine($"weather.gov: {report.City}, {report.State}: " +
                              $"{report.Current?.Description ?? "no current weather"}, {report.Days.Count} forecast days.");
        return Commands(report, celsius, context.Log, context.Characters);
    }

    /// <summary>Makes the commands of a report.</summary>
    /// <param name="characters">The characters of the texts. With Ascii, "°" is left out.</param>
    public IReadOnlyList<BaseCommand> Commands(WeatherReport report, bool celsius, TextWriter log,
        CharacterSet characters = CharacterSet.Latin1)
    {
        var values = Values(report, celsius);
        string Printable(string text) => PrevueText.ToPrintable(text, characters);
        var commands = new List<BaseCommand>
            { new WeatherStatusCommand('1', Label, StatusText(Printable(Fill(Status, values)), log)) };

        if (Current)
        {
            if (report.Current == null)
            {
                log.WriteLine("weather.gov: no station near the location has a current observation. " +
                              "The current weather is not sent.");
            }
            else
            {
                var lines = (CurrentLines ?? [.. DefaultCurrentLines]).Select(line => Printable(Fill(line, values))).ToList();
                commands.Add(new WeatherTextCommand(Minutes, 1, report.Current.Icon ?? 1,
                    string.IsNullOrEmpty(Label) ? "*" : Label, lines));
            }
        }

        if (Forecast && report.Days.Count > 0)
        {
            commands.Add(new WeatherForecastCommand(Minutes, "*", report.Days.Select(day =>
                new WeatherForecastDay(day.Date, day.Icon, Temperature(day.High, celsius),
                    Temperature(day.Low, celsius))).ToList()));
        }

        return commands;
    }

    private (double Latitude, double Longitude) Location()
    {
        if (!string.IsNullOrWhiteSpace(Zip))
            return ZipCodes.Find(Zip) ?? throw new ArgumentException(
                $"The ZIP code \"{Zip}\" is not in the table of the program. Use latitude and longitude instead.");
        if (Latitude is { } latitude && Longitude is { } longitude)
            return (latitude, longitude);
        throw new ArgumentException("!!weatherGov needs a zip, or a latitude and a longitude.");
    }

    private string StatusText(string text, TextWriter log)
    {
        // ESQ accepts a status of 39 bytes: the digit, the label, the separator, the text and the end.
        var maximum = WeatherStatusCommand.MaximumPayloadLength - 3 - Label.Length;
        if (text.Length <= maximum)
            return text;
        log.WriteLine($"weather.gov: the status \"{text}\" is too long. ESQ gets the first {maximum} characters.");
        return text[..maximum].TrimEnd();
    }

    private static Dictionary<string, string> Values(WeatherReport report, bool celsius)
    {
        const string missing = "--";
        var now = report.Current;
        var next = report.Next;
        string Number(double? value) => value is { } v ? Math.Round(v).ToString(CultureInfo.InvariantCulture) : missing;
        double? Convert(double? c) => c is { } v && !celsius ? v * 9 / 5 + 32 : c;

        var wind = missing;
        if (now?.WindSpeedKmh is { } kmh)
        {
            var speed = celsius ? kmh : kmh / 1.609344;
            wind = Math.Round(speed) < 1
                ? "Calm"
                : $"{Compass(now.WindDirectionDegrees)}{Math.Round(speed).ToString(CultureInfo.InvariantCulture)} {(celsius ? "km/h" : "mph")}";
        }

        int? nextTemperature = next?.Temperature is { } f && celsius ? (int)Math.Round((f - 32) * 5 / 9.0) : next?.Temperature;
        return new Dictionary<string, string>
        {
            ["city"] = report.City ?? missing,
            ["state"] = report.State ?? missing,
            ["condition"] = now?.Description ?? missing,
            ["temperature"] = Number(Convert(now?.TemperatureCelsius)),
            ["feelsLike"] = Number(Convert(now?.FeelsLikeCelsius ?? now?.TemperatureCelsius)),
            ["dewPoint"] = Number(Convert(now?.DewPointCelsius)),
            ["pressure"] = now?.PressurePascals is { } pascals
                ? celsius
                    ? $"{Math.Round(pascals / 100).ToString(CultureInfo.InvariantCulture)} hPa"
                    : $"{(pascals / 3386.389).ToString("0.00", CultureInfo.InvariantCulture)} in."
                : missing,
            ["visibility"] = now?.VisibilityMeters is { } meters
                ? celsius
                    ? $"{Math.Round(meters / 1000).ToString(CultureInfo.InvariantCulture)} km"
                    : $"{Math.Round(meters / 1609.344).ToString(CultureInfo.InvariantCulture)} M"
                : missing,
            ["degree"] = "°",
            ["humidity"] = Number(now?.Humidity),
            ["wind"] = wind,
            ["unit"] = celsius ? "C" : "F",
            ["time"] = now?.Time is { } time ? time.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture) : missing,
            ["next"] = next?.Name ?? missing,
            ["nextForecast"] = next?.ShortForecast ?? missing,
            ["nextTemperature"] = nextTemperature?.ToString(CultureInfo.InvariantCulture) ?? missing,
            ["nextHighLow"] = next == null ? missing : next.IsDaytime ? "High" : "Low"
        };
    }

    private static string Compass(double? degrees)
    {
        if (degrees is not { } d)
            return string.Empty;
        string[] points = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];
        return points[(int)Math.Round(((d % 360) + 360) % 360 / 45) % 8] + " ";
    }

    private static int? Temperature(int? fahrenheit, bool celsius) =>
        fahrenheit is { } f && celsius ? (int)Math.Round((f - 32) * 5 / 9.0) : fahrenheit;

    private static string Fill(string template, Dictionary<string, string> values) =>
        Placeholder().Replace(template, match =>
            values.TryGetValue(match.Groups[1].Value, out var value)
                ? value
                : throw new ArgumentException(
                    $"The placeholder {match.Value} of !!weatherGov is not known. The placeholders are " +
                    string.Join(", ", values.Keys.Select(k => $"{{{k}}}")) + "."));

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex Placeholder();
}
