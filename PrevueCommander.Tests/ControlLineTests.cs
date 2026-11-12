using Prevue.Commands;
using Prevue.Commands.Control;
using PrevueCommander.Model;
using PrevueCommander.Model.PlaybookCommands;
using Xunit;

namespace PrevueCommander.Tests;

public class ControlLineTests
{
    private static readonly PlaybookContext Context =
        new(new DateTime(2026, 11, 12, 20, 0, 0), Directory.GetCurrentDirectory(), TextWriter.Null);

    private static string Hex(IEnumerable<BaseCommand> commands) =>
        string.Join(" ", commands.Select(c => Convert.ToHexString(c.Render())));

    [Fact]
    public void Promo_WithOneBox_KeepsTheOtherBrush()
    {
        // The example of docs/ctrl-line.md in Esquire: a promo for Seinfeld on the "around town" brush.
        var commands = Promo.Commands(new PromoBox("Seinfeld", Brush: "AT"), null);
        Assert.Equal("02415430300D1A 115365696E66656C640D26 01312A0D17", Hex(commands));
    }

    [Fact]
    public void Promo_WithTwoBoxes_SendsTheOrderAndTheSeparator()
    {
        var commands = Promo.Commands(new PromoBox("Bob", "KTIV*"), new PromoBox("Seinfeld", Brush: "DT"),
            rightFirst: false);
        Assert.Equal("02303044540D1F 04520D5B 11426F62125365696E66656C640D7B 01314B5449562A122A0D2F", Hex(commands));
    }

    [Fact]
    public void Promo_WithLeftBoxOnly_HasAPatternBeforeTheSeparator()
    {
        var commands = Promo.Commands(null, new PromoBox("Seinfeld"));
        Assert.Equal("\x12Seinfeld", Body(commands[0]));
        Assert.Equal("1*\x12*", Body(commands[1]));
    }

    [Fact]
    public void Promo_ConvertsTheTitleToAscii()
    {
        var commands = Promo.Commands(new PromoBox("  Grey’s Anatomy – Café "), null);
        Assert.Equal("Grey's Anatomy - Cafe", Body(commands[0]));
    }

    [Fact]
    public void Promo_Rejects_BadInput()
    {
        Assert.Throws<ArgumentException>(() => Promo.Commands(null, null));
        Assert.Throws<ArgumentException>(() => Promo.Commands(new PromoBox(" "), null));
        Assert.Throws<ArgumentException>(() => Promo.Commands(new PromoBox("X", Brush: "ABC"), null));
    }

    [Fact]
    public void SubCommands_MatchTheEsquireLauncher()
    {
        Assert.Equal("01330D3F", Convert.ToHexString(new ClearTopHalfCommand().Render()));
        Assert.Equal("01440D48", Convert.ToHexString(new ShowLogoCommand().Render()));
        Assert.Equal("02415441540D0F", Convert.ToHexString(new RawControlCommand(2, "ATAT").Render()));
        Assert.All(new BaseCommand[] { new ClearTopHalfCommand(), new RawControlCommand(4, "L") },
            c => Assert.Equal(SerialLine.Control, c.Line));
    }

    [Fact]
    public void RawPacket_Rejects_BadInput()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RawControlCommand(0, "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RawControlCommand(23, "x"));
        Assert.Throws<ArgumentException>(() => new RawControlCommand(1, "a\rb").Render());
        Assert.Throws<ArgumentException>(() => new RawControlCommand(1, new string('x', 197)).Render());
    }

    [Fact]
    public async Task Playbook_MakesControlLineCommands()
    {
        var playbook = PlaybookLoader.Parse("""
            configuration:
              controlPort: 5402
            commands:
            - !!promo
              right: { title: Seinfeld, brush: AT }
              left: { title: Bob, channels: 'KTIV*' }
              first: Left
            - !!pause
              seconds: 20
            - !!clearTopHalf {}
            - !!showLogo {}
            - !!controlPacket
              type: 17
              body: "Bob\x12Seinfeld"
            """);

        var commands = new List<BaseCommand>();
        foreach (var command in playbook.Commands)
            commands.AddRange(await command.TransformAsync(Context, CancellationToken.None));

        Assert.Equal(5402, playbook.Configuration.ControlPort);
        Assert.False(commands.OfType<PromoOrderCommand>().Single().RightFirst);
        Assert.Equal(TimeSpan.FromSeconds(20), commands.OfType<PauseCommand>().Single().Duration);
        Assert.Equal(SerialLine.None, commands.OfType<PauseCommand>().Single().Line);
        Assert.Equal("11426F62125365696E66656C640D7B", Convert.ToHexString(commands[^1].Render()));
    }

    [Fact]
    public void ControlCommands_NeedAControlPort()
    {
        var lines = new SerialLines(new Configuration(), TextWriter.Null);
        Assert.Throws<InvalidOperationException>(() => lines.Check([new ClearTopHalfCommand()]));
        lines.Check([new SaveCommand(), new PauseCommand(TimeSpan.Zero)]);

        var serial = new SerialLines(new Configuration { ControlSerialPort = "/dev/null" }, TextWriter.Null);
        serial.Check([new ClearTopHalfCommand()]);
    }

    [Fact]
    public void SerialPort_NeedsABaudRate()
    {
        Assert.Throws<ArgumentException>(() => LineConnection.OpenSerial("/dev/null", 0));
    }

    private static string Body(ControlCommand command)
    {
        var bytes = command.Render();
        return new string(bytes[1..^2].Select(b => (char)b).ToArray());
    }
}
