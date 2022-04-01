using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands.CommandObjects;

public record XmlTvFile
{
    [YamlMember]
    public string? Path { get; init; }
    [YamlMember]
    public int MaximumNumberOfChannels { get; init; }
}
