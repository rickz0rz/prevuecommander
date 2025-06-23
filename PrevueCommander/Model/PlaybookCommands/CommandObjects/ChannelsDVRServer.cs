using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands.CommandObjects;

public class ChannelsDVRServer
{
    [YamlMember]
    public string ServerAddress { get; init; }
    [YamlMember]
    public int MaximumNumberOfChannels { get; init; }
}
