using System.Net.Sockets;
using Prevue.Commands;
using PrevueCommander.Model;

namespace PrevueCommander;

/// <summary>
/// Sends each command on its serial line of ESQ. The class connects to a line at its first command, so a playbook
/// with only control line commands does not need the data line.
/// </summary>
public sealed class SerialLines(Configuration configuration, TextWriter log) : IAsyncDisposable
{
    private readonly Dictionary<SerialLine, (TcpClient Client, SerialWriter Writer)> _connections = [];

    /// <summary>
    /// Makes sure that the configuration has a port for each line that <paramref name="commands"/> use.
    /// </summary>
    /// <exception cref="InvalidOperationException">A command needs the control line, and it has no port.</exception>
    public void Check(IEnumerable<BaseCommand> commands)
    {
        if (configuration.ControlPort <= 0 && commands.Any(c => c.Line == SerialLine.Control))
            throw new InvalidOperationException(
                "The playbook has control line commands. Set controlPort in the configuration.");
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
        if (_connections.TryGetValue(line, out var connection))
            return connection.Writer;

        var (port, baudRate, name) = line switch
        {
            SerialLine.Data => (configuration.Port, configuration.BaudRate, "data line"),
            SerialLine.Control => (configuration.ControlPort, configuration.ControlBaudRate, "control line"),
            _ => throw new ArgumentOutOfRangeException(nameof(line), line, "This line has no connection.")
        };

        log.WriteLine($"Connecting to the {name} on {configuration.Hostname}:{port}.");
        var client = new TcpClient();
        try
        {
            await client.ConnectAsync(configuration.Hostname, port, cancellationToken);
        }
        catch
        {
            client.Dispose();
            throw;
        }

        var writer = new SerialWriter(client.GetStream(), baudRate, TimeSpan.FromMilliseconds(configuration.CommandDelay),
            configuration.Output == Output.Verbose, log);
        _connections[line] = (client, writer);
        return writer;
    }

    public ValueTask DisposeAsync()
    {
        foreach (var (client, _) in _connections.Values)
        {
            // Close the send direction first, so that the bridge gets all the bytes before the connection closes.
            try
            {
                client.Client.Shutdown(SocketShutdown.Send);
            }
            catch (SocketException)
            {
                // The bridge closed the connection already.
            }

            client.Dispose();
        }

        _connections.Clear();
        return ValueTask.CompletedTask;
    }
}
