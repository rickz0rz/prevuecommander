using Prevue.Commands;
using Prevue.Commands.Control;
using Prevue.Commands.Listings;
using Prevue.Core;
using PrevueCommander.Model;
using PrevueCommander.Model.PlaybookCommands;

namespace PrevueCommander;

/// <summary>Sends an automatic promo at each interval, with a program from the listings that ESQ has.</summary>
/// <param name="clock">The local time.</param>
/// <param name="send">Sends one command.</param>
/// <param name="characters">The characters of the listings, so that the title finds its program.</param>
public sealed class AutoPromoter(
    AutoPromoSettings settings,
    Func<DateTime> clock,
    Func<BaseCommand, CancellationToken, Task> send,
    TextWriter log,
    CharacterSet characters = CharacterSet.Latin1)
{
    private readonly AutoPromoPicker _picker = new();
    private readonly AutoPromoCriteria _criteria = settings.Criteria();
    private IReadOnlyList<BaseCommand> _listings = [];

    /// <summary>Sets the listings that ESQ has, for example after an update of the watch mode.</summary>
    public void SetListings(IReadOnlyList<BaseCommand> listings) => Volatile.Write(ref _listings, listings);

    /// <summary>Sends a promo at once, and then at each interval, until the cancellation.</summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        log.WriteLine($"Automatic promos: a promo each {settings.IntervalSeconds} s. Press Ctrl+C to stop.");
        while (true)
        {
            await ShowNextAsync(cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), cancellationToken);
        }
    }

    /// <summary>Chooses a program and sends its promo.</summary>
    /// <returns>The program, or null if no program fits.</returns>
    public async Task<PromoProgram?> ShowNextAsync(CancellationToken cancellationToken)
    {
        var now = clock();
        var program = _picker.Pick(Volatile.Read(ref _listings), now, _criteria);
        if (program == null)
        {
            log.WriteLine($"{now:HH:mm:ss} No program fits the automatic promo.");
            return null;
        }

        log.WriteLine($"{now:HH:mm:ss} Promo: {program.Title} on {program.CallLetters} {program.Number} " +
                      $"at {program.Start:HH:mm}.");
        var box = new PromoBox(program.Search, program.CallLetters.Length > 0 ? program.CallLetters : "*",
            settings.Brush);
        var right = settings.Side == PromoSide.Right;
        foreach (var command in Promo.Commands(right ? box : null, right ? null : box, right, settings.Style,
                     characters))
            await send(command, cancellationToken);

        return program;
    }
}
