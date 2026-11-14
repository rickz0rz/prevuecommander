using System.Diagnostics;
using System.Globalization;
using Prevue.Commands;
using PrevueCommander.Model;

namespace PrevueCommander;

/// <summary>
/// Shows the total bytes of a run, the bytes sent and the expected end time. The time of a command is its bytes at the
/// speed of its line, the delay after it and its pause.
/// </summary>
public sealed class TransferProgress
{
    private const int BitsPerByte = 10;

    private readonly Configuration _configuration;
    private readonly TextWriter _log;
    private readonly Func<DateTime> _clock;
    private readonly TimeSpan _interval;
    private readonly Stopwatch _sinceReport = new();
    private long _bytesSent;
    private TimeSpan _timeLeft;

    /// <param name="interval">The time between two reports.</param>
    /// <param name="clock">The local time. The default is the clock of this computer.</param>
    public TransferProgress(IReadOnlyList<BaseCommand> commands, Configuration configuration, TextWriter log,
        TimeSpan interval, Func<DateTime>? clock = null)
    {
        _configuration = configuration;
        _log = log;
        _interval = interval;
        _clock = clock ?? (() => DateTime.Now);
        TotalBytes = commands.Sum(c => (long)c.Render().Length);
        TotalTime = commands.Aggregate(TimeSpan.Zero, (sum, c) => sum + Time(c));
        _timeLeft = TotalTime;
    }

    /// <summary>The bytes of all the commands, on all the lines.</summary>
    public long TotalBytes { get; }

    /// <summary>The expected time of all the commands.</summary>
    public TimeSpan TotalTime { get; }

    /// <summary>The expected time of one command.</summary>
    public TimeSpan Time(BaseCommand command)
    {
        if (command is PauseCommand pause)
            return pause.Duration;

        var baudRate = command.Line == SerialLine.Control ? _configuration.ControlBaudRate : _configuration.BaudRate;
        var send = baudRate > 0
            ? TimeSpan.FromSeconds((double)command.Render().Length * BitsPerByte / baudRate)
            : TimeSpan.Zero;
        return send + TimeSpan.FromMilliseconds(Math.Max(0, _configuration.CommandDelay));
    }

    /// <summary>Shows the total bytes and the expected time before the first command.</summary>
    public void Start()
    {
        _log.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"The run sends {TotalBytes:N0} bytes. It takes about {Format(TotalTime)}. " +
            $"Expected end: {_clock() + TotalTime:HH:mm:ss}."));
        _sinceReport.Restart();
    }

    /// <summary>Counts a command that was sent, and shows the progress at each interval.</summary>
    public void Sent(BaseCommand command)
    {
        _bytesSent += command.Render().Length;
        _timeLeft -= Time(command);
        if (_timeLeft < TimeSpan.Zero)
            _timeLeft = TimeSpan.Zero;

        if (_sinceReport.Elapsed < _interval || _bytesSent >= TotalBytes)
            return;

        _sinceReport.Restart();
        _log.WriteLine(Report());
    }

    /// <summary>The progress as text, for example "Sent 1,200 of 4,800 bytes (25%). Expected end: 19:40:12."</summary>
    public string Report()
    {
        var percent = TotalBytes == 0 ? 100 : (int)(_bytesSent * 100 / TotalBytes);
        return string.Create(CultureInfo.InvariantCulture,
            $"Sent {_bytesSent:N0} of {TotalBytes:N0} bytes ({percent}%). Expected end: {_clock() + _timeLeft:HH:mm:ss}.");
    }

    /// <summary>A time as hours, minutes and seconds, for example "1 h 5 min 3 s" or "12 s".</summary>
    public static string Format(TimeSpan time)
    {
        var seconds = (long)Math.Ceiling(time.TotalSeconds);
        var hours = seconds / 3600;
        var minutes = seconds % 3600 / 60;
        seconds %= 60;
        var parts = new List<string>();
        if (hours > 0)
            parts.Add($"{hours} h");
        if (hours > 0 || minutes > 0)
            parts.Add($"{minutes} min");
        parts.Add(string.Create(CultureInfo.InvariantCulture, $"{seconds} s"));
        return string.Join(' ', parts);
    }
}
