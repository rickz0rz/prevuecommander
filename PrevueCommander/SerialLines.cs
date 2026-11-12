using Prevue.Commands;
using PrevueCommander.Model;

namespace PrevueCommander;

/// <summary>
/// Sends each command on its serial line of ESQ, through a TCP bridge or a serial port. The class connects to a line at
/// its first command, so a playbook with only control line commands does not need the data line.
/// </summary>
public sealed class SerialLines(Configuration configuration, TextWriter log) : IAsyncDisposable
{
    private readonly Dictionary<SerialLine, (LineConnection Connection, SerialWriter Writer)> _connections = [];

    /// <summary>
    /// Makes sure that the configuration has a port for each line that <paramref name="commands"/> use.
    /// </summary>
    /// <exception cref="InvalidOperationException">A command needs the control line, and it has no port.</exception>
    public void Check(IEnumerable<BaseCommand> commands)
    {
        if (configuration.ControlPort <= 0 && string.IsNullOrEmpty(configuration.ControlSerialPort) &&
            commands.Any(c => c.Line == SerialLine.Control))
            throw new InvalidOperationException(
                "The playbook has control line commands. Set controlPort or controlSerialPort in the configuration.");
    }

    public async Task SendAsync(BaseCommand command, CancellationToken cancellationToken)
    {
        if (command is PauseCommand pause)
        {
            log.WriteLine($"[{pause}]");
            await Task.Delay(pause.Duration, cancellationToken);
            return;
        }

        var writer = await WriterAsync(command.Line, cancellationToken);
        await writer.SendAsync(command, cancellationToken);
    }

    private async Task<SerialWriter> WriterAsync(SerialLine line, CancellationToken cancellationToken)
    {
        if (_connections.TryGetValue(line, out var open))
            return open.Writer;

        var (serialPort, tcpPort, baudRate, name) = line switch
        {
            SerialLine.Data => (configuration.SerialPort, configuration.Port, configuration.BaudRate, "data line"),
            SerialLine.Control => (configuration.ControlSerialPort, configuration.ControlPort,
                configuration.ControlBaudRate, "control line"),
            _ => throw new ArgumentOutOfRangeException(nameof(line), line, "This line has no connection.")
        };

        LineConnection connection;
        if (!string.IsNullOrEmpty(serialPort))
        {
            log.WriteLine($"Opening the serial port {serialPort} at {baudRate} baud for the {name}.");
            connection = LineConnection.OpenSerial(serialPort, baudRate);
        }
        else
        {
            log.WriteLine($"Connecting to the {name} on {configuration.Hostname}:{tcpPort}.");
            connection = await LineConnection.OpenTcpAsync(configuration.Hostname, tcpPort, cancellationToken);
        }

        var writer = new SerialWriter(connection.Stream, baudRate, TimeSpan.FromMilliseconds(configuration.CommandDelay),
            configuration.Output == Output.Verbose, log);
        _connections[line] = (connection, writer);
        return writer;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (connection, _) in _connections.Values)
            await connection.DisposeAsync();

        _connections.Clear();
    }
}
