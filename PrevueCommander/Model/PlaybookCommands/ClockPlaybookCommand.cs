using Prevue.Commands;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands;

public record ClockPlaybookCommand : IBasePlaybookCommand
{
    [YamlMember]
    public bool UseCurrentDate { get; init; }

    [YamlMember]
    public string Date { get; set; }

    public Task<List<BaseCommand>> Transform()
    {
        return Task.FromResult(new List<BaseCommand> { new ClockCommand(UseCurrentDate ? DateTime.Now : DateTime.Parse(Date)) });
    }
}
