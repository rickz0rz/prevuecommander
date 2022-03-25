using Prevue.Commands;
using PrevueCommander.XmlTv;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands;

public record LocalAdsPlaybookCommand : IBasePlaybookCommand
{
    [YamlMember(Alias = "ads")]
    public List<string> Ads { get; init; }
    public Task<List<BaseCommand>> Transform()
    {
        var commands = new List<BaseCommand>
        {
            new LocalAdResetCommand()
        };

        for (var i = 0; i < Ads.Count; i++)
        {
            commands.Add(new LocalAdCommand(i, Ads[i]));
        }

        return Task.FromResult(commands);
    }
}
