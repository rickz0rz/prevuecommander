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
}
