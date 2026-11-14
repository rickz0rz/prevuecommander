using Prevue.Commands;
using PrevueCommander.Model.PlaybookCommands;

namespace PrevueCommander;

/// <summary>
/// Reads the weather of the !!weatherGov commands of the playbook again, and sends it in a session. The weather
/// mode does this at an interval. The web server does it on request. If weather.gov cannot be read, the watcher shows
/// the error, keeps the last weather of ESQ, and tries again at the next interval.
/// </summary>
/// <param name="readers">The !!weatherGov commands of the playbook.</param>
/// <param name="contextAt">Makes the context of a read at a time.</param>
/// <param name="clock">The local time.</param>
/// <param name="send">Sends the commands of an update together.</param>
public sealed class WeatherWatcher(
    IReadOnlyList<WeatherGovPlaybookCommand> readers,
    Func<DateTime, PlaybookContext> contextAt,
    Func<DateTime> clock,
    Func<IReadOnlyList<BaseCommand>, CancellationToken, Task> send,
    TextWriter log)
{
    private readonly SessionTemplate _session = new();
    private readonly SemaphoreSlim _update = new(1, 1);

    /// <summary>The time of the last successful read.</summary>
    public DateTime? LastRead { get; private set; }

    /// <summary>The error of the last read, or null if it was successful.</summary>
    public string? LastError { get; private set; }

    /// <summary>Remembers the session commands of the first run.</summary>
    public void Start(IReadOnlyList<BaseCommand> sent) => _session.Remember(sent);

    /// <summary>Waits for the interval, and sends the weather, until the cancellation.</summary>
    public async Task RunAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        log.WriteLine($"Weather mode: weather.gov is read again each {interval.TotalMinutes:0.#} min. Press Ctrl+C to stop.");
        while (true)
        {
            await Task.Delay(interval, cancellationToken);
            await UpdateAsync(cancellationToken);
        }
    }

    /// <summary>Reads the weather and sends it.</summary>
    /// <returns>True if the weather was sent.</returns>
    public async Task<bool> UpdateAsync(CancellationToken cancellationToken)
    {
        await _update.WaitAsync(cancellationToken);
        try
        {
            var now = clock();
            var context = contextAt(now);
            var commands = new List<BaseCommand>();
            try
            {
                foreach (var reader in readers)
                    commands.AddRange(await reader.TransformAsync(context, cancellationToken));
            }
            catch (Exception e) when (e is HttpRequestException or InvalidDataException or System.Text.Json.JsonException
                                          or ArgumentException
                                          or OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                // A timeout of HttpClient is a cancellation without the cancellation of the program.
                LastError = e is OperationCanceledException ? "weather.gov did not answer in time." : e.Message;
                log.WriteLine($"{now:HH:mm:ss} The weather cannot be read: {LastError} ESQ keeps the last weather.");
                return false;
            }

            await send(_session.Wrap(commands), cancellationToken);
            LastRead = now;
            LastError = null;
            log.WriteLine($"{now:HH:mm:ss} Sent the weather of weather.gov.");
            return true;
        }
        finally
        {
            _update.Release();
        }
    }
}
