using Prevue.Commands;
using Prevue.Commands.Listings;
using Prevue.Core;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// The settings and the steps that the guide data imports share. An import sends the listings of the current and
/// the next broadcast day of ESQ. ESQ needs a configuration command before the channel lineup.
/// </summary>
public abstract record GuideDataImportPlaybookCommand : IPlaybookCommand
{
    /// <summary>
    /// True to send the channel lineup. Set it to false to update only the programs of a lineup that ESQ already
    /// has. If the lineup changed, ESQ deletes the programs of the day, so all the programs must follow it.
    /// </summary>
    public bool SendChannelLineUp { get; init; } = true;

    /// <summary>The order of the channels in the grid.</summary>
    public SortMode ChannelNumberOrder { get; init; } = SortMode.None;

    /// <summary>Channel numbers or call signs of the channels to show on a red background.</summary>
    public List<string> PremiumChannels { get; init; } = [];

    /// <summary>Channel numbers or call signs of the channels to leave out.</summary>
    public List<string> ExcludedChannels { get; init; } = [];

    public async Task<IReadOnlyList<BaseCommand>> TransformAsync(PlaybookContext context,
        CancellationToken cancellationToken)
    {
        var channels = await LoadChannelsAsync(context, cancellationToken);
        var excluded = new HashSet<string>(ExcludedChannels, StringComparer.OrdinalIgnoreCase);
        var premium = new HashSet<string>(PremiumChannels, StringComparer.OrdinalIgnoreCase);
        var selected = channels
            .Where(c => !excluded.Contains(c.Number) && !excluded.Contains(c.CallSign))
            .Select(c => c with { Premium = premium.Contains(c.Number) || premium.Contains(c.CallSign) })
            .SortByChannelNumber(ChannelNumberOrder, c => c.Number)
            .ToList();

        if (selected.Count > ChannelLineUpCommand.MaximumChannels)
        {
            context.Log.WriteLine($"The guide has {selected.Count} channels. ESQ keeps a maximum of " +
                                  $"{ChannelLineUpCommand.MaximumChannels}, so only the first " +
                                  $"{ChannelLineUpCommand.MaximumChannels} are sent.");
        }

        return ListingsBuilder.Build(selected, context.Now, SendChannelLineUp);
    }

    /// <summary>The channels of all the sources, each source already limited to its maximum number of channels.</summary>
    protected abstract Task<List<GuideChannel>> LoadChannelsAsync(PlaybookContext context,
        CancellationToken cancellationToken);

    /// <summary>Sorts the channels of one source and takes the first ones.</summary>
    protected IEnumerable<GuideChannel> Limit(IEnumerable<GuideChannel> channels, int maximumNumberOfChannels) =>
        channels.SortByChannelNumber(ChannelNumberOrder, c => c.Number)
            .Take(maximumNumberOfChannels > 0 ? maximumNumberOfChannels : ChannelLineUpCommand.MaximumChannels);
}
