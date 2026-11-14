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

    /// <summary>Shows the lines of the ad as ESQ shows them, and a warning for text that ESQ breaks or drops.</summary>
    private static void ShowPreview(LocalAdCommand ad, TextWriter log)
    {
        var lines = LocalAdLayout.Lines(ad.EncodedText);
        log.WriteLine($"Local ad {ad.Number}:");
        foreach (var line in lines.Take(LocalAdLayout.MaximumLines))
            log.WriteLine($"  {LocalAdLayout.Preview(line)}");
        foreach (var problem in LocalAdLayout.Problems(lines))
            log.WriteLine($"  Warning: {problem}");
    }
}
