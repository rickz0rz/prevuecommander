using Prevue.Commands.Listings;
using Prevue.Core;
using PrevueCommander.ChannelsDVR;
using PrevueCommander.Model.PlaybookCommands.CommandObjects;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Imports the guide of one or more Channels DVR servers.</summary>
public record ChannelsDVRGuideDataImportPlaybookCommand : GuideDataImportPlaybookCommand
{
    public List<ChannelsDVRServer> ChannelsDVRServers { get; init; } = [];

    protected override async Task<List<GuideChannel>> LoadChannelsAsync(PlaybookContext context,
        CancellationToken cancellationToken)
    {
        // The guide from the start of the current broadcast day to the end of the next one.
        var start = BroadcastDay.StartOf(BroadcastDay.Current(context.Now));
        var channels = new List<GuideChannel>();
        foreach (var server in ChannelsDVRServers)
        {
            using var client = new ChannelsDvrClient(server.ServerAddress);
            var guide = await client.GetGuideAsync(start, TimeSpan.FromDays(2), cancellationToken);
            var serverChannels = ChannelsDvrGuide.Convert(guide, server.FilterOnlyHDChannels,
                server.FilterMusicChoiceChannels);
            channels.AddRange(Limit(serverChannels, server.MaximumNumberOfChannels));
        }

        return channels;
    }
}
