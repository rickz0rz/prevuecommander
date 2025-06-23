using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands.CommandObjects;

public class ChannelsDVRServer
{
    // Add:
    // Filter out SD channels
    // Filter out MusicChoice channels
    // Deduplicate channels on channel number (useful for multiple servers)
    // Deduplicate channels on callsign
    // Deduplicate channels on channel name (and prefer HD channels)

    [YamlMember] public string ServerAddress { get; init; }

    [YamlMember] public int MaximumNumberOfChannels { get; init; }

    [YamlMember] public bool? FilterOnlyHDChannels { get; init; }
}