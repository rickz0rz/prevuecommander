using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

public record BoxOffPlaybookCommand : IBasePlaybookCommand
{
    public Task<List<BaseCommand>> Transform()
    {
        return Task.FromResult(new List<BaseCommand> { new BoxOffCommand() });
    }
}
