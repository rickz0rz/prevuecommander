using YamlDotNet.Serialization;

namespace PrevueCommander.Model;

public enum Output
{
    Minimal,
    Verbose
}

public record Configuration
{
    [YamlMember]
    public string Hostname { get; init; }
    [YamlMember]
    public int Port { get; init; }
    [YamlMember]
    public Output Output { get; init; }
}
