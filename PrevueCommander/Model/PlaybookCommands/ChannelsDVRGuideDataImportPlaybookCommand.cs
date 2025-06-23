using Prevue.Commands;
using Prevue.Commands.Model;
using Prevue.Core;
using PrevueCommander.ChannelsDVR;
using PrevueCommander.Model.PlaybookCommands.CommandObjects;
using YamlDotNet.Serialization;
// using PrevueCommander.XmlTv;
// using PrevueCommander.XmlTv.Model;

namespace PrevueCommander.Model.PlaybookCommands;

public class ChannelsDVRGuideDataImportPlaybookCommand : IBasePlaybookCommand
{
    [YamlMember] public List<ChannelsDVRServer>? ChannelsDVRServers { get; init; }

    [YamlMember] public bool SendChannelLineUp { get; init; }

    [YamlMember] public SortMode ChannelNumberOrder { get; init; }

    public async Task<List<BaseCommand>> Transform()
    {
        var commands = new List<BaseCommand>();
        var now = DateTime.Now;

        foreach (var channelsDvrServer in ChannelsDVRServers ?? new List<ChannelsDVRServer>())
        {
            var programCommands = new List<ChannelProgramCommand>();

            var api = new ChannelsAPI(channelsDvrServer.ServerAddress);
            var guide = await api.GetGuide();

            var channelsSelection = guide.RootElement
                .EnumerateArray()
                .Select(guideElement => guideElement.GetProperty("Channel"))
                .Where(channelElement => channelElement.TryGetProperty("Station", out _));

            if (channelsDvrServer.FilterOnlyHDChannels.HasValue && channelsDvrServer.FilterOnlyHDChannels.Value)
                channelsSelection = channelsSelection
                    .Where(channelElement => channelElement.TryGetProperty("HD", out _))
                    .Where(channelElement => channelElement.GetProperty("HD").GetBoolean());

            var channels = channelsSelection
                .Where(channelElement => !channelElement.GetProperty("CallSign").GetString().StartsWith("MC"))
                .Select(channelElement => new Channel
                {
                    CallSign = channelElement.GetProperty("CallSign").GetString(),
                    ChannelNumber = channelElement.GetProperty("Number").GetString(),
                    SourceName = NormalizeSourceName(channelElement.GetProperty("CallSign").GetString())
                })
                .DistinctBy(channel => channel.ChannelNumber)
                .DistinctBy(channel => channel.CallSign);

            var knownSources = SortChannels(channels, ChannelNumberOrder)
                .Take(channelsDvrServer.MaximumNumberOfChannels);

            foreach (var guideElement in guide.RootElement.EnumerateArray())
            {
                var channelElement = guideElement.GetProperty("Channel");
                foreach (var airingElement in guideElement.GetProperty("Airings").EnumerateArray())
                {
                    var programCommand =
                        ChannelsCore.GenerateProgramCommand(now, knownSources, channelElement, airingElement);
                    if (programCommand != null) programCommands.Add(programCommand);
                }
            }

            if (SendChannelLineUp) commands.Add(new ChannelLineUpCommand(now, knownSources));

            commands.AddRange(programCommands);
        }

        return commands;
    }

    private IEnumerable<Channel> SortChannels(IEnumerable<Channel> channels, SortMode sortMode)
    {
        return sortMode switch
        {
            SortMode.None => channels,
            SortMode.Ascending => channels.OrderBy(c => int.Parse(c.ChannelNumber)),
            SortMode.Descending => channels.OrderByDescending(c => int.Parse(c.ChannelNumber)),
            _ => channels
        };
    }

    private string NormalizeSourceName(string sourceName)
    {
        return sourceName.EndsWith("HD")
            ? sourceName[..^"HD".Length]
            : sourceName;
    }
}