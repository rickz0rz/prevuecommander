using Prevue.Commands;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands;

public record AdditionalConfigurationPlaybookCommand : IBasePlaybookCommand
{
    [YamlMember(Alias = "payload")]
    public byte Payload { get; init; }

    public Task<List<BaseCommand>> Transform()
    {
        return Task.FromResult(new List<BaseCommand> { new AdditionalConfigurationCommand(Payload) });
    }
}
