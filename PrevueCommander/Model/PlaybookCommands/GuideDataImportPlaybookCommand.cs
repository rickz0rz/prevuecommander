using Prevue.Commands;
using PrevueCommander.Model.PlaybookCommands.CommandObjects;
using PrevueCommander.XmlTv;
using PrevueCommander.XmlTv.Model;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands;

public enum SortMode
{
    None,
    Ascending,
    Descending
}

public record GuideDataImportPlaybookCommand : IBasePlaybookCommand
{
    private IEnumerable<Channel> SortChannels(IEnumerable<Channel> channels, SortMode sortMode)
    {
        return sortMode switch
        {
            SortMode.None => channels,
            SortMode.Ascending => channels.OrderBy(c => c.ChannelNumber),
            SortMode.Descending => channels.OrderByDescending(c => c.ChannelNumber),
            _ => channels
        };
    }

    [YamlMember]
    public List<XmlTvFile> XmlTvFiles { get; init; }
    [YamlMember]
    public bool SendChannelLineUp { get; init; }
    [YamlMember]
    public SortMode ChannelNumberOrder { get; init; }

    public async Task<List<BaseCommand>> Transform()
    {
        var channels = new List<Channel>();
        var programCommands = new List<ChannelProgramCommand>();
        var now = DateTime.Now;

        foreach (var xmlTvFile in XmlTvFiles)
        {
            var xmlTvObject = await XmlTvCore.ParseXmlFile(xmlTvFile.Path);
            var targetChannels = SortChannels(xmlTvObject.Channel, ChannelNumberOrder).ToList();
            channels.AddRange(targetChannels);

            var knownSources = targetChannels.Select(tc => tc.SourceName).ToList();

            foreach (var programme in xmlTvObject.Programme)
            {
                var programCommand = XmlTvCore.GenerateProgramCommand(now, knownSources, programme);
                if (programCommand != null)
                {
                    programCommands.Add(programCommand);
                }
            }
        }

        var commands = new List<BaseCommand>();

        if (SendChannelLineUp)
        {
            var channelsToSend = SortChannels(channels, ChannelNumberOrder)
                .Select(c => new Prevue.Commands.Model.Channel
            {
                CallSign = c.CallSign,
                ChannelNumber = c.ChannelNumber,
                SourceName = c.SourceName
            });

            commands.Add(new ChannelLineUpCommand(now, channelsToSend));
        }

        commands.AddRange(programCommands);

        return commands;
    }
}
