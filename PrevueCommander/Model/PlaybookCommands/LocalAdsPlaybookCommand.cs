using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Deletes the local ads of ESQ and sends new ones. ESQ needs a configuration command before it.</summary>
public record LocalAdsPlaybookCommand : SimplePlaybookCommand
{
    public List<string> Ads { get; init; } = [];

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context)
    {
        if (Ads.Count > LocalAdCommand.MaximumAds)
            throw new InvalidOperationException(
                $"The playbook has {Ads.Count} local ads. ESQ keeps a maximum of {LocalAdCommand.MaximumAds}.");

        var commands = Ads.Select((ad, i) => new LocalAdCommand(i + 1, ad, context.Characters)).ToList();
        foreach (var ad in commands)
            ShowPreview(ad, context.Log);

        return [new ResetLocalAdsCommand(), .. commands];
    }

    /// <summary>The local ad commands of the ads, with the reset command first.</summary>
    public IReadOnlyList<BaseCommand> Commands(PlaybookContext context) => Transform(context);

    /// <summary>The lines of an ad as ESQ shows them, and the text that ESQ breaks or drops.</summary>
    public static LocalAdPreview Preview(LocalAdCommand ad)
    {
        var lines = LocalAdLayout.Lines(ad.EncodedText);
        return new LocalAdPreview(ad.Number,
            lines.Take(LocalAdLayout.MaximumLines).Select(LocalAdLayout.Preview).ToList(),
            LocalAdLayout.Problems(lines));
    }

    /// <summary>Shows the lines of the ad as ESQ shows them, and a warning for text that ESQ breaks or drops.</summary>
    private static void ShowPreview(LocalAdCommand ad, TextWriter log)
    {
        var preview = Preview(ad);
        log.WriteLine($"Local ad {ad.Number}:");
        foreach (var line in preview.Lines)
            log.WriteLine($"  {line}");
        foreach (var warning in preview.Warnings)
            log.WriteLine($"  Warning: {warning}");
    }
}

/// <summary>The preview of a local ad.</summary>
/// <param name="Number">The number of the ad, from 1.</param>
/// <param name="Lines">The lines that ESQ shows, each between two '|'.</param>
/// <param name="Warnings">The text that ESQ breaks or drops.</param>
public sealed record LocalAdPreview(int Number, IReadOnlyList<string> Lines, IReadOnlyList<string> Warnings);
