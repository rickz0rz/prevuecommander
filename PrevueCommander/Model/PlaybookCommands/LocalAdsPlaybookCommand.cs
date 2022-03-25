using Prevue.Commands;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands;

public record LocalAdsPlaybookCommand : IBasePlaybookCommand
{
    [YamlMember]
    public List<string> Ads { get; init; }
    public Task<List<BaseCommand>> Transform()
    {
        var commands = new List<BaseCommand>
        {
            new LocalAdResetCommand()
        };

        commands.AddRange(Ads.Select((t, i) => new LocalAdCommand(i, t)));

        return Task.FromResult(commands);
    }
}
