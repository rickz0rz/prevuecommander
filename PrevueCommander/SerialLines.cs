using Prevue.Commands;
using PrevueCommander.Model;

namespace PrevueCommander;

/// <summary>Sends groups of commands to ESQ.</summary>
public interface ICommandSink
{
    /// <summary>
    /// Sends the commands in order. Another sender cannot put a command of the same line between them, so a session
    /// or a promo stays complete.
    /// </summary>
    /// <param name="sent">Called after each command, for example to count the progress.</param>
    Task SendBatchAsync(IReadOnlyList<BaseCommand> commands, Action<BaseCommand>? sent,
        CancellationToken cancellationToken);
}

/// <summary>
/// Sends each command on its serial line of ESQ, through a TCP bridge or a serial port. The class connects to a line at
/// its first command, so a playbook with only control line commands does not need the data line.
/// </summary>
/// <remarks>
/// The watch mode, the automatic promos and the web server can send at the same time. Each line has a lock. A batch
/// keeps the lock of a line for all its consecutive commands on that line.
/// </remarks>
public sealed class SerialLines(Configuration configuration, TextWriter log) : IAsyncDisposable, ICommandSink
{
    private readonly Dictionary<SerialLine, (LineConnection Connection, SerialWriter Writer)> _connections = [];

    // Only one task opens a connection.
    private readonly SemaphoreSlim _open = new(1, 1);

    private readonly Dictionary<SerialLine, SemaphoreSlim> _lineLocks = new()
    {
        [SerialLine.Data] = new SemaphoreSlim(1, 1),
        [SerialLine.Control] = new SemaphoreSlim(1, 1)
    };

    private long _dataBytesSent;
    private long _controlBytesSent;

    /// <summary>The bytes that were sent on a line.</summary>
    public long BytesSent(SerialLine line) => line switch
    {
        SerialLine.Data => Interlocked.Read(ref _dataBytesSent),
        SerialLine.Control => Interlocked.Read(ref _controlBytesSent),
        _ => 0
    };

    /// <summary>True if the line has a connection.</summary>
    public bool IsConnected(SerialLine line)
    {
        lock (_connections)
            return _connections.ContainsKey(line);
    }

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

    /// <summary>Sends one command.</summary>
    public Task SendAsync(BaseCommand command, CancellationToken cancellationToken) =>
        SendBatchAsync([command], null, cancellationToken);

    /// <inheritdoc />
    public async Task SendBatchAsync(IReadOnlyList<BaseCommand> commands, Action<BaseCommand>? sent,
        CancellationToken cancellationToken)
    {
        var i = 0;
        while (i < commands.Count)
        {
            if (commands[i] is PauseCommand pause)
            {
                log.WriteLine($"[{pause}]");
                await Task.Delay(pause.Duration, cancellationToken);
                sent?.Invoke(pause);
                i++;
                continue;
            }

            // Keep the lock of the line for the consecutive commands of that line.
            var line = commands[i].Line;
            var gate = _lineLocks[line];
            await gate.WaitAsync(cancellationToken);
            try
            {
                var writer = await WriterAsync(line, cancellationToken);
                for (; i < commands.Count && commands[i] is not PauseCommand && commands[i].Line == line; i++)
                {
                    await writer.SendAsync(commands[i], cancellationToken);
                    var bytes = commands[i].Render().Length;
                    if (line == SerialLine.Data)
                        Interlocked.Add(ref _dataBytesSent, bytes);
                    else
                        Interlocked.Add(ref _controlBytesSent, bytes);
                    sent?.Invoke(commands[i]);
                }
            }
            finally
            {
                gate.Release();
            }
        }
    }

    private async Task<SerialWriter> WriterAsync(SerialLine line, CancellationToken cancellationToken)
    {
        await _open.WaitAsync(cancellationToken);
        try
        {
            return await OpenAsync(line, cancellationToken);
        }
        finally
        {
            _open.Release();
        }
    }

    private async Task<SerialWriter> OpenAsync(SerialLine line, CancellationToken cancellationToken)
    {
        lock (_connections)
        {
            if (_connections.TryGetValue(line, out var open))
                return open.Writer;
        }

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
        lock (_connections)
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
