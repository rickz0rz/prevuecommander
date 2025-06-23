using Prevue.Commands;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands;

public record ClockPlaybookCommand : IBasePlaybookCommand
{
    [YamlMember] public bool UseCurrentDate { get; init; }

    [YamlMember] public string? Date { get; set; }

    public Task<List<BaseCommand>> Transform()
    {
        var clockCommand = new ClockCommand(UseCurrentDate || !string.IsNullOrWhiteSpace(Date)
            ? DateTime.Now.AddHours(1)
            : DateTime.Parse(Date));

        return Task.FromResult(new List<BaseCommand>
        {
            clockCommand
        });
    }
}