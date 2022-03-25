using PrevueCommander.Model.PlaybookCommands;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model;

public record Playbook
{
    [YamlMember(Alias = "config")]
    public Configuration Configuration { get; init; } = null!;

    [YamlMember(Alias = "commands")]
    public List<IBasePlaybookCommand> Commands { get; init; } = null!;
}
