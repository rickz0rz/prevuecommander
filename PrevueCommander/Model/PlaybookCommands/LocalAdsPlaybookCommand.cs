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

        return [new ResetLocalAdsCommand(), .. Ads.Select((ad, i) => new LocalAdCommand(i + 1, ad))];
    }
}
