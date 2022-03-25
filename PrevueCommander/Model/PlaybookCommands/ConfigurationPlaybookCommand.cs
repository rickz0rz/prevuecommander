using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

public record ConfigurationPlaybookCommand : IBasePlaybookCommand
{
    public Task<List<BaseCommand>> Transform()
    {
        return Task.FromResult(new List<BaseCommand> { new ConfigurationCommand() });
    }
}
