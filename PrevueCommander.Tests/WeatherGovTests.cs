using System.Net;
using System.Text;
using Prevue.Commands;
using PrevueCommander.Model.PlaybookCommands;
using PrevueCommander.Server;
using PrevueCommander.WeatherGov;
using Xunit;

namespace PrevueCommander.Tests;

public class WeatherGovTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 7, 0, 0);

    private const string Point = """
        {"properties": {
          "forecast": "https://api.weather.gov/gridpoints/DTX/66,34/forecast",
          "observationStations": "https://api.weather.gov/gridpoints/DTX/66,34/stations",
          "relativeLocation": {"properties": {"city": "Detroit", "state": "MI"}},
          "timeZone": "America/Detroit"}}
        """;

    private const string Forecast = """
        {"properties": {"periods": [
          {"name": "Today", "startTime": "2026-11-14T06:00:00-05:00", "isDaytime": true, "temperature": 46,
           "icon": "https://api.weather.gov/icons/land/day/bkn?size=medium", "shortForecast": "Partly Sunny"},
          {"name": "Tonight", "startTime": "2026-11-14T18:00:00-05:00", "isDaytime": false, "temperature": 34,
           "icon": "https://api.weather.gov/icons/land/night/bkn?size=medium", "shortForecast": "Mostly Cloudy"},
          {"name": "Sunday", "startTime": "2026-11-15T06:00:00-05:00", "isDaytime": true, "temperature": 48,
           "icon": "https://api.weather.gov/icons/land/day/rain,40/bkn?size=medium", "shortForecast": "Chance Rain"},
          {"name": "Sunday Night", "startTime": "2026-11-15T18:00:00-05:00", "isDaytime": false, "temperature": 31,
           "icon": "https://api.weather.gov/icons/land/night/sct?size=medium", "shortForecast": "Partly Cloudy"},
          {"name": "Monday", "startTime": "2026-11-16T06:00:00-05:00", "isDaytime": true, "temperature": 50,
           "icon": "https://api.weather.gov/icons/land/day/few?size=medium", "shortForecast": "Sunny"},
          {"name": "Monday Night", "startTime": "2026-11-16T18:00:00-05:00", "isDaytime": false, "temperature": 31,
           "icon": "https://api.weather.gov/icons/land/night/few?size=medium", "shortForecast": "Mostly Clear"},
          {"name": "Tuesday", "startTime": "2026-11-17T06:00:00-05:00", "isDaytime": true, "temperature": 30,
           "icon": "https://api.weather.gov/icons/land/day/snow?size=medium", "shortForecast": "Snow"},
          {"name": "Tuesday Night", "startTime": "2026-11-17T18:00:00-05:00", "isDaytime": false, "temperature": -2,
           "icon": "https://api.weather.gov/icons/land/night/snow?size=medium", "shortForecast": "Snow"}]}}
        """;

    private const string Stations = """
        {"features": [{"properties": {"stationIdentifier": "KDET"}}, {"properties": {"stationIdentifier": "KDTW"}}]}
        """;

    private const string Observation = """
        {"properties": {"timestamp": "2026-11-14T11:10:00+00:00", "textDescription": "Cloudy",
          "icon": "https://api.weather.gov/icons/land/night/ovc?size=medium",
          "temperature": {"unitCode": "wmoUnit:degC", "value": 1},
          "dewpoint": {"unitCode": "wmoUnit:degC", "value": -1},
          "barometricPressure": {"unitCode": "wmoUnit:Pa", "value": 101625.52},
          "visibility": {"unitCode": "wmoUnit:m", "value": 16093.44},
          "relativeHumidity": {"unitCode": "wmoUnit:percent", "value": 86.4987},
          "windDirection": {"unitCode": "wmoUnit:degree_(angle)", "value": 290},
          "windSpeed": {"unitCode": "wmoUnit:km_h-1", "value": 7.416},
          "windChill": {"unitCode": "wmoUnit:degC", "value": -1.36},
          "heatIndex": {"unitCode": "wmoUnit:degC", "value": null}}}
        """;

    /// <summary>Answers the requests of the weather.gov client from a table of paths.</summary>
    private sealed class FakeWeatherGov(Dictionary<string, string> answers) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var path = request.RequestUri!.PathAndQuery;
            return Task.FromResult(answers.TryGetValue(path, out var json)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8) }
                : new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
        }
    }

    private static FakeWeatherGov Fake(bool firstStationWorks = true)
    {
        var answers = new Dictionary<string, string>
        {
            ["/points/42.347,-83.06"] = Point,
            ["/gridpoints/DTX/66,34/forecast"] = Forecast,
            ["/gridpoints/DTX/66,34/stations"] = Stations,
            ["/stations/KDTW/observations/latest"] = Observation
        };
        if (firstStationWorks)
            answers["/stations/KDET/observations/latest"] = Observation;
        return new FakeWeatherGov(answers);
    }

    private static WeatherReport Report() => WeatherReport.Build(
        System.Text.Json.JsonSerializer.Deserialize<Feature>(Point)!.Properties,
        System.Text.Json.JsonSerializer.Deserialize<ForecastFeature>(Forecast)!.Properties.Periods,
        System.Text.Json.JsonSerializer.Deserialize<ObservationFeature>(Observation)!.Properties,
        DateOnly.FromDateTime(Now));

    private sealed record Feature(
        [property: System.Text.Json.Serialization.JsonPropertyName("properties")] WeatherGovPoint Properties);

    private sealed record PeriodList(
        [property: System.Text.Json.Serialization.JsonPropertyName("periods")] WeatherGovPeriod[] Periods);

    private sealed record ForecastFeature(
        [property: System.Text.Json.Serialization.JsonPropertyName("properties")] PeriodList Properties);

    private sealed record ObservationFeature(
        [property: System.Text.Json.Serialization.JsonPropertyName("properties")] WeatherGovObservation Properties);

    [Theory]
    [InlineData("48201")]
    [InlineData(" 48201-1234 ")]
    public void ZipCodes_FindTheCenterOfTheZipCode(string zip)
    {
        var (latitude, longitude) = ZipCodes.Find(zip)!.Value;
        Assert.Equal(42.347, latitude, 3);
        Assert.Equal(-83.06, longitude, 3);
    }

    [Theory]
    [InlineData("00000")]
    [InlineData("4820")]
    [InlineData("abcde")]
    public void ZipCodes_ReturnNullForAnUnknownZipCode(string zip) => Assert.Null(ZipCodes.Find(zip));

    [Theory]
    [InlineData("https://api.weather.gov/icons/land/day/few?size=medium", null, 2)]
    [InlineData("https://api.weather.gov/icons/land/night/skc?size=medium", null, 2)]
    [InlineData("https://api.weather.gov/icons/land/day/ovc?size=medium", null, 3)]
    [InlineData("https://api.weather.gov/icons/land/day/rain,40/bkn?size=medium", null, 4)]
    [InlineData("https://api.weather.gov/icons/land/day/tsra_hi,60?size=medium", null, 4)]
    [InlineData("https://api.weather.gov/icons/land/day/bkn/rain,30?size=medium", null, 5)]
    [InlineData("https://api.weather.gov/icons/land/day/blizzard?size=medium", null, 6)]
    [InlineData("https://api.weather.gov/icons/land/day/unknowncode?size=medium", "Light Snow", 6)]
    [InlineData(null, "Mostly Sunny", 5)]
    [InlineData(null, "Fog/Mist", 3)]
    [InlineData(null, null, null)]
    public void Icons_UseTheFirstConditionOfTheLink_ThenTheText(string? link, string? text, int? icon) =>
        Assert.Equal(icon, WeatherIcons.Choose(link, text));

    [Fact]
    public void Report_HasTheNext3Days_WithTheHighOfTheDayAndTheLowOfTheNight()
    {
        var report = Report();

        Assert.Equal("Detroit", report.City);
        Assert.Equal("Today", report.Next!.Name);
        Assert.Equal([
            new WeatherDay(new DateOnly(2026, 11, 15), WeatherIcons.Rain, 48, 31, "Chance Rain"),
            new WeatherDay(new DateOnly(2026, 11, 16), WeatherIcons.Sun, 50, 31, "Sunny"),
            new WeatherDay(new DateOnly(2026, 11, 17), WeatherIcons.Snow, 30, -2, "Snow")
        ], report.Days);
        Assert.Equal(WeatherIcons.Clouds, report.Current!.Icon);
    }

    [Fact]
    public void Commands_HaveTheStatus_TheCurrentWeather_AndTheForecast()
    {
        var commands = new WeatherGovPlaybookCommand { Zip = "48201" }.Commands(Report(), false, TextWriter.Null);

        var status = Assert.IsType<WeatherStatusCommand>(commands[0]);
        Assert.Equal(('1', "LOCAL", "Detroit"), (status.Digit, status.Label, status.Text));
        var text = Assert.IsType<WeatherTextCommand>(commands[1]);
        // The "current conditions" of Prevue Weather, with the values of the left column first in each pair.
        Assert.Equal(["Cloudy", "Temp 34°F", "Wind W 5 mph", "Pressure 30.01 in.", "Humidity 86%", "DewPoint 30°F",
            "Visibility 10 M", ""], text.Lines);
        Assert.Equal((90, WeatherIcons.Clouds, "LOCAL"), (text.Minutes, text.Icon, text.LabelPattern));
        var forecast = Assert.IsType<WeatherForecastCommand>(commands[2]);
        Assert.Equal([48, 50, 30], forecast.Days.Select(d => d.High));
    }

    [Fact]
    public void Commands_UseCelsius_AndTheLinesOfThePlaybook()
    {
        var command = new WeatherGovPlaybookCommand
        {
            Zip = "48201",
            Units = "C",
            Forecast = false,
            CurrentLines = ["{temperature}{unit}, feels {feelsLike}", "{wind}", "{next}: {nextTemperature}"]
        };

        var commands = command.Commands(Report(), true, TextWriter.Null);

        Assert.Equal(2, commands.Count);
        Assert.Equal(["1C, feels -1", "W 7 km/h", "Today: 8"], Assert.IsType<WeatherTextCommand>(commands[1]).Lines);
    }

    [Fact]
    public void Commands_UseHectopascalsAndKilometers_AndLeaveOutTheDegreeSignInAscii()
    {
        var command = new WeatherGovPlaybookCommand { Forecast = false, CurrentLines = ["{temperature}{degree}{unit}", "{pressure}", "{visibility}"] };

        var lines = Assert.IsType<WeatherTextCommand>(
            command.Commands(Report(), true, TextWriter.Null, Prevue.Core.CharacterSet.Ascii)[1]).Lines;

        Assert.Equal(["1C", "1016 hPa", "16 km"], lines);
    }

    [Fact]
    public void Commands_RejectAnUnknownPlaceholder() =>
        Assert.Throws<ArgumentException>(() => new WeatherGovPlaybookCommand { CurrentLines = ["{rain}"] }
            .Commands(Report(), false, TextWriter.Null));

    [Fact]
    public void Commands_ShortenALongStatusForESQ()
    {
        var command = new WeatherGovPlaybookCommand { Status = "{city}, {state} - the weather of weather.gov" };

        var status = Assert.IsType<WeatherStatusCommand>(command.Commands(Report(), false, TextWriter.Null)[0]);

        // 39 bytes: the digit, "LOCAL", the separator, the text and the end.
        Assert.Equal(31, status.Text.Length);
    }

    [Fact]
    public async Task Playbook_ReadsTheZipCode_AndTriesTheNextStation()
    {
        var fake = Fake(firstStationWorks: false);
        WeatherGovPlaybookCommand.ClientFactory = contact => new WeatherGovClient(contact, fake);
        var playbook = PlaybookLoader.Parse("""
            commands:
            - !!weatherGov
              zip: '48201'
              contact: weather@example.com
            """);

        var commands = await playbook.Commands[0].TransformAsync(
            new PlaybookContext(Now, Directory.GetCurrentDirectory(), TextWriter.Null),
            TestContext.Current.CancellationToken);

        Assert.Equal([typeof(WeatherStatusCommand), typeof(WeatherTextCommand), typeof(WeatherForecastCommand)],
            commands.Select(c => c.GetType()));
        Assert.Contains(fake.Requests, r => r.RequestUri!.AbsolutePath == "/stations/KDTW/observations/latest");
        var agent = fake.Requests[0].Headers.UserAgent.ToString();
        Assert.Contains("PrevueCommander", agent);
        Assert.Contains("weather@example.com", agent);
    }

    [Fact]
    public async Task Playbook_RejectsAMissingLocation()
    {
        var command = new WeatherGovPlaybookCommand();
        await Assert.ThrowsAsync<ArgumentException>(() => command.TransformAsync(
            new PlaybookContext(Now, Directory.GetCurrentDirectory(), TextWriter.Null),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Watcher_SendsASession_OrKeepsTheErrorAndSendsNothing()
    {
        var sent = new List<IReadOnlyList<BaseCommand>>();
        var fake = Fake();
        WeatherGovPlaybookCommand.ClientFactory = contact => new WeatherGovClient(contact, fake);
        var watcher = new WeatherWatcher([new WeatherGovPlaybookCommand { Zip = "48201" }],
            time => new PlaybookContext(time, Directory.GetCurrentDirectory(), TextWriter.Null), () => Now,
            (batch, _) =>
            {
                sent.Add(batch);
                return Task.CompletedTask;
            }, TextWriter.Null);
        watcher.Start([new AddressCommand("12345")]);

        Assert.True(await watcher.UpdateAsync(TestContext.Current.CancellationToken));
        Assert.IsType<AddressCommand>(sent[0][0]);
        Assert.IsType<BoxOffCommand>(sent[0][^1]);

        WeatherGovPlaybookCommand.ClientFactory = contact => new WeatherGovClient(contact, new FakeWeatherGov([]));
        Assert.False(await watcher.UpdateAsync(TestContext.Current.CancellationToken));
        Assert.Single(sent);
        Assert.Contains("404", watcher.LastError);
        Assert.Equal(Now, watcher.LastRead);
    }

    [Fact]
    public async Task Server_SendsTheWeatherOfAZipCode_AndShowsTheForecast()
    {
        var fake = Fake();
        WeatherGovPlaybookCommand.ClientFactory = contact => new WeatherGovClient(contact, fake);
        var (server, sink, jobs) = MakeServer();

        var (status, _) = await server.HandleAsync("POST", "/prevue/data/weather-gov",
            """{"zip": "48201", "show": "forecast", "pauseSeconds": 0}""", TestContext.Current.CancellationToken);
        Assert.Equal(202, status);
        var (refresh, _) = await server.HandleAsync("POST", "/prevue/weather/refresh", "",
            TestContext.Current.CancellationToken);
        Assert.Equal(409, refresh);

        using var stop = new CancellationTokenSource();
        var run = jobs.RunAsync(stop.Token);
        while (jobs.Waiting() != (0, 0))
            await Task.Delay(10, TestContext.Current.CancellationToken);
        await stop.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);

        var batch = Assert.Single(sink.Batches);
        Assert.Equal([typeof(AddressCommand), typeof(ConfigurationCommand), typeof(WeatherStatusCommand),
            typeof(WeatherTextCommand), typeof(WeatherForecastCommand), typeof(BoxOffCommand), typeof(PauseCommand),
            typeof(ShowWeatherCommand)], batch.Select(c => c.GetType()));
        Assert.True(((ShowWeatherCommand)batch[^1]).Forecast);
    }

    [Fact]
    public async Task Server_RejectsABadShowValue()
    {
        var (server, _, _) = MakeServer();
        var (status, _) = await server.HandleAsync("POST", "/prevue/data/weather-gov",
            """{"zip": "48201", "show": "radar"}""", TestContext.Current.CancellationToken);
        Assert.Equal(400, status);
    }

    private sealed class Sink : ICommandSink
    {
        public List<IReadOnlyList<BaseCommand>> Batches { get; } = [];

        public Task SendBatchAsync(IReadOnlyList<BaseCommand> commands, Action<BaseCommand>? sent,
            CancellationToken cancellationToken)
        {
            Batches.Add(commands);
            return Task.CompletedTask;
        }
    }

    private static (ControlServer Server, Sink Sink, JobQueue Jobs) MakeServer()
    {
        var sink = new Sink();
        var jobs = new JobQueue(sink, _ => TimeSpan.FromSeconds(1), () => Now, TextWriter.Null);
        var hold = new TopHalfHold(jobs, () => Now, TextWriter.Null, TestContext.Current.CancellationToken);
        var parts = new ServerParts(() => new PlaybookContext(Now, Path.GetTempPath(), TextWriter.Null),
            new SessionTemplate(), _ => { }, _ => (false, 0), 110);
        return (new ControlServer(parts, jobs, hold, () => Now, TextWriter.Null), sink, jobs);
    }
}
