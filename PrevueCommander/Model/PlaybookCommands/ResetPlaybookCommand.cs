using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

public class ResetPlaybookCommand : IBasePlaybookCommand
{
    public Task<List<BaseCommand>> Transform()
    {
        return Task.FromResult(new List<BaseCommand> { new ResetCommand() });
    }
}