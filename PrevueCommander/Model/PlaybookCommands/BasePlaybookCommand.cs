using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

public interface IBasePlaybookCommand
{
    Task<List<BaseCommand>> Transform();
}