using Prevue.Commands;
using PrevueCommander.Model.PlaybookCommands;
using Xunit;

namespace PrevueCommander.Tests;

public class WeatherTests
{
    private static string Body(BaseCommand command)
    {
        var bytes = command.Render();
        // Leave out 55 AA, the NUL and the checksum.
        return string.Concat(bytes[2..^2].Select(b => (char)b));
    }

    [Fact]
    public void Status_HasTheDigit_TheLabel_AndTheText() =>
        Assert.Equal("I1DETROIT\u0012Detroit", Body(new WeatherStatusCommand('1', "DETROIT", "Detroit")));

    [Fact]
    public void Status_AcceptsAPayloadOf39BytesWithTheEnd()
    {
        // 1 + 7 + 1 + 29 + the NUL = 39 bytes. A test in Esquire showed no line error for it.
        _ = new WeatherStatusCommand('1', "DETROIT", new string('a', 29));
        Assert.Throws<ArgumentException>(() => new WeatherStatusCommand('1', "DETROIT", new string('a', 30)));
    }

    [Theory]
    [InlineData('a', "LABEL", "Text")]
    [InlineData('1', "ELEVENCHARS", "Text")]
    [InlineData('1', "LABEL", "Line\nbreak")]
    public void Status_RejectsBadValues(char digit, string label, string text) =>
        Assert.ThrowsAny<ArgumentException>(() => new WeatherStatusCommand(digit, label, text));

    [Fact]
    public void Text_EndsEachLineWithALineBreak()
    {
        var command = new WeatherTextCommand(30, 1, 2, "DET*", ["Clear", "Low 32"]);
        Assert.Equal("W\x1E\x01\x02" + "0DET*\x12" + "Clear\x18Low 32\x18", Body(command));
    }

    [Theory]
    [InlineData(0, 1, 1, "*", 1)]
    [InlineData(256, 1, 1, "*", 1)]
    [InlineData(30, 0, 1, "*", 1)]
    [InlineData(30, 1, 7, "*", 1)]
    [InlineData(30, 1, 1, "", 1)]
    [InlineData(30, 1, 1, "*", 0)]
    [InlineData(30, 1, 1, "*", 11)]
    public void Text_RejectsBadValues(int minutes, int color, int icon, string label, int lines) =>
        Assert.ThrowsAny<ArgumentException>(() => new WeatherTextCommand(minutes, color, icon, label,
            Enumerable.Repeat("Line", lines).ToList()));

    [Fact]
    public void ShowWeather_IsControlType1WithW0() =>
        Assert.Equal("\x01W0\x0D", string.Concat(new ShowWeatherCommand().Render()[..^1].Select(b => (char)b)));

    [Fact]
    public async Task Playbook_ReadsTheWeatherCommands()
    {
        var playbook = PlaybookLoader.Parse("""
            commands:
            - !!weatherStatus
              digit: '1'
              label: DETROIT
              text: Detroit, Michigan
            - !!weatherText
              minutes: 30
              icon: 2
              label: DETROIT
              lines: [Tonight - Clear, Low 32]
            - !!showWeather {}
            """);
        var context = new PlaybookContext(DateTime.Now, Directory.GetCurrentDirectory(), TextWriter.Null);

        var commands = new List<BaseCommand>();
        foreach (var command in playbook.Commands)
            commands.AddRange(await command.TransformAsync(context, TestContext.Current.CancellationToken));

        Assert.Equal("DETROIT", Assert.IsType<WeatherStatusCommand>(commands[0]).Label);
        var text = Assert.IsType<WeatherTextCommand>(commands[1]);
        Assert.Equal(2, text.Icon);
        Assert.Equal(["Tonight - Clear", "Low 32"], text.Lines);
        Assert.IsType<ShowWeatherCommand>(commands[2]);
    }

    [Fact]
    public void Decoder_ShowsTheWeatherFields()
    {
        byte[] feed = [.. new WeatherStatusCommand('1', "DETROIT", "Detroit").Render(),
            .. new WeatherTextCommand(30, 1, 2, "DET*", ["Clear", "Low 32"]).Render()];

        var commands = FeedDecoder.DecodeData(feed).ToList();

        Assert.Contains(("label", "\"DETROIT\""), commands[0].Fields);
        Assert.Contains(("text", "\"Detroit\""), commands[0].Fields);
        Assert.Contains(("icon", "2"), commands[1].Fields);
        Assert.Contains(("line 2", "\"Low 32\""), commands[1].Fields);
        Assert.DoesNotContain(commands[1].Fields, f => f.Name == "dropped");
    }

    [Fact]
    public void Forecast_HasTheLabel_TheMinutes_AndEachDay()
    {
        var command = new WeatherForecastCommand(30, "*", [
            new WeatherForecastDay(new DateOnly(2026, 11, 15), 2, 81, 51),
            new WeatherForecastDay(new DateOnly(2026, 1, 1), null, -5, null)]);

        // A test in Esquire showed that ESQ finds a day by its day of the year, with 1 for January 1.
        Assert.Equal("w*\x12\x1E" + "+3192081051" + "+001?-05???", Body(command));
    }

    [Theory]
    [InlineData(0, "*", 2, 80, 0)]
    [InlineData(256, "*", 2, 80, 1)]
    [InlineData(30, "", 2, 80, 1)]
    [InlineData(30, "*", 1, 80, 1)]
    [InlineData(30, "*", 7, 80, 1)]
    [InlineData(30, "*", 2, 1000, 1)]
    [InlineData(30, "*", 2, -100, 1)]
    [InlineData(30, "*", 2, 80, 5)]
    public void Forecast_RejectsBadValues(int minutes, string label, int icon, int high, int days)
    {
        var list = Enumerable.Range(0, days)
            .Select(i => new WeatherForecastDay(new DateOnly(2026, 11, 15 + i), icon, high, 50)).ToList();
        Assert.ThrowsAny<ArgumentException>(() => new WeatherForecastCommand(minutes, label, list));
    }

    [Fact]
    public void Forecast_Accepts0Days_Never() =>
        Assert.Throws<ArgumentException>(() => new WeatherForecastCommand(30, "*", []));

    [Fact]
    public async Task Playbook_ForecastDaysStartTomorrow_AndShowWeatherSendsW3()
    {
        var playbook = PlaybookLoader.Parse("""
            commands:
            - !!weatherForecast
              minutes: 30
              days:
              - {icon: 2, high: 81, low: 51}
              - {high: 82}
              - {date: 2026-12-25, icon: 6, high: 30, low: 20}
            - !!showWeather
              forecast: true
            """);
        var context = new PlaybookContext(new DateTime(2026, 11, 14, 6, 0, 0), Directory.GetCurrentDirectory(),
            TextWriter.Null);

        var commands = new List<BaseCommand>();
        foreach (var command in playbook.Commands)
            commands.AddRange(await command.TransformAsync(context, TestContext.Current.CancellationToken));

        var forecast = Assert.IsType<WeatherForecastCommand>(commands[0]);
        Assert.Equal([new DateOnly(2026, 11, 15), new DateOnly(2026, 11, 16), new DateOnly(2026, 12, 25)],
            forecast.Days.Select(d => d.Date));
        Assert.Equal(new WeatherForecastDay(new DateOnly(2026, 11, 16), null, 82, null), forecast.Days[1]);
        Assert.Equal("\x01W3\x0D", string.Concat(commands[1].Render()[..^1].Select(b => (char)b)));
    }

    [Fact]
    public void Decoder_ShowsTheForecastDays()
    {
        var feed = new WeatherForecastCommand(30, "*", [new WeatherForecastDay(new DateOnly(2026, 11, 15), 2, 81, 51)])
            .Render();

        var command = Assert.Single(FeedDecoder.DecodeData(feed));

        Assert.Contains(("minutes", "30"), command.Fields);
        Assert.Contains(("day 1", "\"3192081051\""), command.Fields);
        Assert.DoesNotContain(command.Fields, f => f.Name == "dropped");
    }
}
