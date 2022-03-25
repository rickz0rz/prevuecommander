using PrevueCommander.Model.PlaybookCommands;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model;

public record Playbook
{
    [YamlMember]
    public Configuration Configuration { get; init; } = null!;

    [YamlMember]
    public List<IBasePlaybookCommand> Commands { get; init; } = null!;
}
