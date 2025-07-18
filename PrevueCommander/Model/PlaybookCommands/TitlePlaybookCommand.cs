using Prevue.Commands;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands;

public record TitlePlaybookCommand : IBasePlaybookCommand
{
    [YamlMember] public string? Text { get; init; }

    public Task<List<BaseCommand>> Transform()
    {
        return Task.FromResult(new List<BaseCommand> { new TitleCommand(Text ?? string.Empty) });
    }
}