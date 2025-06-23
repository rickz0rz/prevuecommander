using Prevue.Commands;
using Prevue.Commands.Model;
using Prevue.Core;
using PrevueCommander.ChannelsDVR;
using PrevueCommander.Model.PlaybookCommands.CommandObjects;
// using PrevueCommander.XmlTv;
// using PrevueCommander.XmlTv.Model;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands;

public class ChannelsDVRGuideDataImportPlaybookCommand : IBasePlaybookCommand
{
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

    [YamlMember]
    public List<ChannelsDVRServer>? ChannelsDVRServers { get; init; }
    [YamlMember]
    public bool SendChannelLineUp { get; init; }
    [YamlMember]
    public SortMode ChannelNumberOrder { get; init; }

    public async Task<List<BaseCommand>> Transform()
    {
        var commands = new List<BaseCommand>();

        foreach (var channelsDvrServer in ChannelsDVRServers ?? new List<ChannelsDVRServer>())
        {
            var api = new ChannelsAPI(channelsDvrServer.ServerAddress);

            var c = await api.GetChannels();

            var channels = c.RootElement
                .EnumerateArray()
                .Where(channelElement => channelElement.TryGetProperty("station_id", out var _))
                .Select(channelElement => new Channel
                {
                    CallSign = channelElement.GetProperty("name").GetString(),
                    ChannelNumber = channelElement.GetProperty("number").GetString(),
                    SourceName = channelElement.GetProperty("station_id").GetString()
                })
                .DistinctBy(channel => channel.ChannelNumber);

            var guide = await api.GetGuide();
            var programCommands = new List<ChannelProgramCommand>();
            var now = DateTime.Now;

            var knownSources = SortChannels(channels, ChannelNumberOrder)
                .Take(channelsDvrServer.MaximumNumberOfChannels);

            foreach (var guideElement in guide.RootElement.EnumerateArray())
            {
                var channelElement = guideElement.GetProperty("Channel");
                foreach (var airingElement in guideElement.GetProperty("Airings").EnumerateArray())
                {
                    var programCommand =
                        ChannelsCore.GenerateProgramCommand(now, knownSources, channelElement, airingElement);
                    if (programCommand != null)
                    {
                        programCommands.Add(programCommand);
                    }
                }
            }

            if (SendChannelLineUp)
            {
                commands.Add(new ChannelLineUpCommand(now, knownSources));
            }

            commands.AddRange(programCommands);
        }

        return commands;
    }
}
