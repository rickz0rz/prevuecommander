using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

public record ConfigureDstPlaybookCommand : IBasePlaybookCommand
{
    public Task<List<BaseCommand>> Transform()
    {
        return Task.FromResult(new List<BaseCommand> { new ConfigureDstCommand() });
    }
}