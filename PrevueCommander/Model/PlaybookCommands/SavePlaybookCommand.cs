using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

public class SavePlaybookCommand : IBasePlaybookCommand
{
    public Task<List<BaseCommand>> Transform()
    {
        return Task.FromResult(new List<BaseCommand> { new SaveCommand() });
    }
}