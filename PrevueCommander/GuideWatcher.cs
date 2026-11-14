using Prevue.Commands;
using Prevue.Commands.Listings;
using PrevueCommander.Model.PlaybookCommands;

namespace PrevueCommander;

/// <summary>
/// The watch mode: reads the guide of the imports of the playbook again at an interval, and sends only the changes.
/// </summary>
/// <remarks>
/// Each update with changes is a session: the address and the status packet of the playbook, the changes, a save
/// command if the playbook has one, and a box off command. An update does not send the channel logos again. If the
/// guide cannot be read, the watcher shows the error and tries again at the next interval.
/// </remarks>
/// <param name="imports">The guide data imports of the playbook.</param>
/// <param name="contextAt">Makes the context of an update for a local time.</param>
/// <param name="clock">The local time of an update.</param>
/// <param name="send">Sends the commands of one session together.</param>
/// <param name="log">The output for messages.</param>
public sealed class GuideWatcher(
    IReadOnlyList<GuideDataImportPlaybookCommand> imports,
    Func<DateTime, PlaybookContext> contextAt,
    Func<DateTime> clock,
    Func<IReadOnlyList<BaseCommand>, CancellationToken, Task> send,
    TextWriter log)
{
    private readonly ListingsState _state = new();
    private readonly SessionTemplate _session = new();

    // The watch loop and the web server can start an update at the same time. One update runs at a time.
    private readonly SemaphoreSlim _update = new(1, 1);

    /// <summary>The local time of the last read of the guide, or null before the first read.</summary>
    public DateTime? LastRead { get; private set; }

    /// <summary>The listings commands that the last update sent.</summary>
    public int LastChanges { get; private set; }

    /// <summary>The error of the last read, or null if the guide was read.</summary>
    public string? LastError { get; private set; }

    /// <summary>Gets all the listings of each read of the guide, for example for the automatic promos.</summary>
    public Action<IReadOnlyList<BaseCommand>>? ListingsRead { get; init; }

    /// <summary>
    /// Remembers the commands of the first run: the listings that ESQ has, and the session commands to use again.
    /// </summary>
    public void Start(IReadOnlyList<BaseCommand> sent)
    {
        _state.Remember(sent);
        _session.Remember(sent);
    }

    /// <summary>Waits for the interval, and updates the listings, until the cancellation.</summary>
    public async Task RunAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        log.WriteLine($"Watch mode: the guide is read again each {interval.TotalMinutes:0.#} min. Press Ctrl+C to stop.");
        while (true)
        {
            await Task.Delay(interval, cancellationToken);
            await UpdateAsync(cancellationToken);
        }
    }

    /// <summary>Reads the guide and sends the changes.</summary>
    /// <returns>The number of listings commands that were sent.</returns>
    public async Task<int> UpdateAsync(CancellationToken cancellationToken)
    {
        await _update.WaitAsync(cancellationToken);
        try
        {
            return await UpdateOnceAsync(cancellationToken);
        }
        finally
        {
            _update.Release();
        }
    }

    private async Task<int> UpdateOnceAsync(CancellationToken cancellationToken)
    {
        var now = clock();
        LastRead = now;
        List<BaseCommand> listings;
        try
        {
            listings = await ReadAsync(now, cancellationToken);
            LastError = null;
        }
        catch (Exception e) when (e is HttpRequestException or IOException or InvalidDataException or FormatException
                                       or InvalidOperationException or UnauthorizedAccessException ||
                                   e is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            log.WriteLine($"{now:HH:mm:ss} The guide cannot be read: {e.Message} The next update tries again.");
            LastError = e.Message;
            LastChanges = 0;
            return 0;
        }

        ListingsRead?.Invoke(listings);
        var changes = _state.Changes(listings);
        LastChanges = changes.Count;
        if (changes.Count == 0)
        {
            log.WriteLine($"{now:HH:mm:ss} The guide has no changes.");
            return 0;
        }

        log.WriteLine($"{now:HH:mm:ss} The guide changed. Sending {changes.Count} listings " +
                      (changes.Count == 1 ? "command." : "commands."));
        await send(_session.Wrap(changes), cancellationToken);

        return changes.Count;
    }

    private async Task<List<BaseCommand>> ReadAsync(DateTime now, CancellationToken cancellationToken)
    {
        var context = contextAt(now);
        var listings = new List<BaseCommand>();
        foreach (var import in imports)
            listings.AddRange((await import.ImportAsync(context, false, cancellationToken)).Commands);
        return listings;
    }
}
