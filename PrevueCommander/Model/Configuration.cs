using YamlDotNet.Serialization;

namespace PrevueCommander.Model;

public record Configuration
{
    [YamlMember(Alias = "hostname")]
    public string Hostname { get; init; }
    [YamlMember(Alias = "port")]
    public int Port { get; init; }
}
