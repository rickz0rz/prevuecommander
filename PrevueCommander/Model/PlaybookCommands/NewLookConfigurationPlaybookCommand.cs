using Prevue.Commands;
using PrevueCommander.Model.PlaybookCommands.CommandObjects;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands;

public record NewLookConfigurationPlaybookCommand : IBasePlaybookCommand
{
    [YamlMember]
    public NewLookConfiguration Configuration { get; init; }

    public Task<List<BaseCommand>> Transform()
    {
        return Task.FromResult(new List<BaseCommand> {new NewLookConfigurationCommand()});
    }
}
