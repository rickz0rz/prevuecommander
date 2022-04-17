using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

public class ReloadPlaybookCommand: IBasePlaybookCommand
{
    public Task<List<BaseCommand>> Transform()
    {
        return Task.FromResult(new List<BaseCommand> { new UtilityCommand(3, "3", "6") });
    }
}
