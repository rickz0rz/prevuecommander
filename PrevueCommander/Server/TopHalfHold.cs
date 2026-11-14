using Prevue.Commands.Control;

namespace PrevueCommander.Server;

/// <summary>The state of the hold, for the HTTP API.</summary>
/// <param name="Until">The local time when the hold ends, or null for no end.</param>
public sealed record HoldInfo(bool Active, int IntervalSeconds, DateTime? Started, DateTime? Until,
    DateTime? NextClear);

/// <summary>
/// Keeps the top half of the screen clear, so that the genlock video shows. It sends a clear at once and then at each
/// interval.
/// </summary>
/// <remarks>
/// ESQ shows the next logo of LOGO.LST 180 seconds after the last clear. A clear at intervals of less than 3 minutes
/// thus stops the logo rotation, and LOGO.LST does not change. A test with Esquire with a clear each 150 seconds
/// showed no logo in 8 minutes. When the hold ends, the rotation starts again.
/// </remarks>
public sealed class TopHalfHold(JobQueue jobs, Func<DateTime> clock, TextWriter log, CancellationToken shutdown)
{
    public const int DefaultIntervalSeconds = 150;
    public const int MinimumIntervalSeconds = 5;
    public const int MaximumIntervalSeconds = 170;

    private readonly Lock _lock = new();
    private CancellationTokenSource? _loop;
    private HoldInfo _info = new(false, DefaultIntervalSeconds, null, null, null);

    public HoldInfo Info
    {
        get
        {
            lock (_lock)
                return _info;
        }
    }

    /// <summary>Starts a hold, or replaces the current hold.</summary>
    /// <param name="minutes">The time of the hold. Null holds until <see cref="Stop"/>.</param>
    public HoldInfo Start(double? minutes, int intervalSeconds)
    {
        if (intervalSeconds is < MinimumIntervalSeconds or > MaximumIntervalSeconds)
            throw new ArgumentException(
                $"The interval must be {MinimumIntervalSeconds} to {MaximumIntervalSeconds} seconds. ESQ shows the " +
                "next logo 180 seconds after a clear.");
        if (minutes is <= 0)
            throw new ArgumentException("The minutes of a hold must be more than 0.");

        lock (_lock)
        {
            _loop?.Cancel();
            _loop = CancellationTokenSource.CreateLinkedTokenSource(shutdown);
            var now = clock();
            _info = new HoldInfo(true, intervalSeconds, now, minutes == null ? null : now.AddMinutes(minutes.Value),
                now);
            _ = RunAsync(_info, _loop.Token);
            log.WriteLine(_info.Until == null
                ? $"Hold: the top half stays clear until a stop. A clear each {intervalSeconds} s."
                : $"Hold: the top half stays clear until {_info.Until:HH:mm:ss}. A clear each {intervalSeconds} s.");
            return _info;
        }
    }

    /// <summary>Stops the hold. The logo rotation starts again.</summary>
    /// <returns>False if no hold was active.</returns>
    public bool Stop()
    {
        lock (_lock)
        {
            if (!_info.Active)
                return false;
            _loop?.Cancel();
            _loop = null;
            _info = _info with { Active = false, NextClear = null };
            log.WriteLine("Hold: stopped. The logo rotation starts again.");
            return true;
        }
    }

    private async Task RunAsync(HoldInfo hold, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                jobs.Enqueue("Keep the top half clear", [new ClearTopHalfCommand()]);
                var next = clock().AddSeconds(hold.IntervalSeconds);
                if (hold.Until is { } until && next >= until)
                    break;

                lock (_lock)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    _info = _info with { NextClear = next };
                }

                await Task.Delay(TimeSpan.FromSeconds(hold.IntervalSeconds), cancellationToken);
            }

            // The last clear holds the top half until about 3 minutes after it. The hold then ends.
            var end = hold.Until!.Value;
            if (end > clock())
                await Task.Delay(end - clock(), cancellationToken);
            lock (_lock)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _info = _info with { Active = false, NextClear = null };
                    log.WriteLine("Hold: ended. The logo rotation starts again.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // A stop, a new hold or the end of the program.
        }
    }
}
