using PrevueCommander.Model;
using Xunit;

namespace PrevueCommander.Tests;

public class CommandLineTests
{
    [Fact]
    public void NoArguments_UsesTheDefaultPlaybook()
    {
        var options = CommandLine.Parse([]);
        Assert.Equal("playbook.yaml", options.PlaybookPath);
        Assert.False(options.Help);
        Assert.False(options.Validate);
    }

    [Fact]
    public void OptionsAndPlaybook_AreRead()
    {
        var options = CommandLine.Parse(["--port", "5401", "--baud-rate=0", "my.yaml", "--validate", "--verbose",
            "--now", "1993-02-23T18:00", "--host", "10.0.0.2"]);
        Assert.Equal("my.yaml", options.PlaybookPath);
        Assert.Equal(5401, options.Port);
        Assert.Equal(0, options.BaudRate);
        Assert.True(options.Validate);
        Assert.True(options.Verbose);
        Assert.Equal("1993-02-23T18:00", options.Now);
        Assert.Equal("10.0.0.2", options.Hostname);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public void Help_IsRead(string arg) => Assert.True(CommandLine.Parse([arg]).Help);

    [Theory]
    [InlineData("--unknown")]
    [InlineData("--port")]
    [InlineData("--port", "abc")]
    [InlineData("--port", "-1")]
    [InlineData("--help=yes")]
    [InlineData("one.yaml", "two.yaml")]
    public void BadArguments_AreAnError(params string[] args) =>
        Assert.Throws<CommandLineException>(() => CommandLine.Parse(args));

    [Fact]
    public void Apply_ReplacesOnlyTheSettingsThatAreSet()
    {
        var playbook = new Configuration { Port = 5400, BaudRate = 2400, CommandDelay = 50, Now = "2020-01-01T00:00" };
        var configuration = CommandLine.Parse(["--port", "5401", "--verbose"]).Apply(playbook);
        Assert.Equal(5401, configuration.Port);
        Assert.Equal(2400, configuration.BaudRate);
        Assert.Equal(50, configuration.CommandDelay);
        Assert.Equal("2020-01-01T00:00", configuration.Now);
        Assert.Equal(Output.Verbose, configuration.Output);
    }

    [Fact]
    public void Apply_PortReplacesTheSerialPortOfThePlaybook()
    {
        var playbook = new Configuration { SerialPort = "COM3", ControlSerialPort = "COM4" };
        var configuration = CommandLine.Parse(["--port", "5401", "--control-port", "5402"]).Apply(playbook);
        Assert.Null(configuration.SerialPort);
        Assert.Null(configuration.ControlSerialPort);
        Assert.Equal(5402, configuration.ControlPort);
    }

    [Fact]
    public void Apply_SerialPortReplacesTheTcpBridge()
    {
        var configuration = CommandLine.Parse(["--serial-port", "/dev/ttyUSB0"]).Apply(new Configuration());
        Assert.Equal("/dev/ttyUSB0", configuration.SerialPort);
    }
}
