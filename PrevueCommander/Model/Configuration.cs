using YamlDotNet.Serialization;

namespace PrevueCommander.Model;

public record Configuration
{
    [YamlMember]
    public string Hostname { get; init; }
    [YamlMember]
    public int Port { get; init; }
    [YamlMember]
    public bool? VerboseDataOutput { get; init; }
}
