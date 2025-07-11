using Prevue.Commands;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands;

public record AddressPlaybookCommand : IBasePlaybookCommand
{
    [YamlMember] public string? Target { get; init; }

    public Task<List<BaseCommand>> Transform()
    {
        return Task.FromResult(new List<BaseCommand> { new AddressCommand(Target ?? "*") });
    }
}