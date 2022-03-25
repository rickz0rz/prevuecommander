using Prevue.Commands;
using PrevueCommander.XmlTv;
using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands;

public record XmlTvImportPlaybookCommand : IBasePlaybookCommand
{
    [YamlMember]
    public string XmlFile { get; init; }
    [YamlMember]
    public int MaximumNumberOfChannels { get; init; }

    public async Task<List<BaseCommand>> Transform()
    {
        return await XmlTvCore.ImportXml(DateTime.Now, XmlFile, MaximumNumberOfChannels);
    }
}
