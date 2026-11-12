using Prevue.Commands.Listings;
using PrevueCommander.Model.PlaybookCommands.CommandObjects;
using PrevueCommander.XmlTv;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Imports the guide of one or more XMLTV files.</summary>
public record XmlTvGuideDataImportPlaybookCommand : GuideDataImportPlaybookCommand
{
    public List<XmlTvFile> XmlTvFiles { get; init; } = [];

    protected override async Task<List<GuideChannel>> LoadChannelsAsync(PlaybookContext context,
        CancellationToken cancellationToken)
    {
        var channels = new List<GuideChannel>();
        foreach (var file in XmlTvFiles)
        {
            var path = context.ResolvePath(file.Path);
            if (!File.Exists(path))
                throw new FileNotFoundException($"The XMLTV file {path} does not exist.", path);

            var tv = await XmlTvGuide.ParseFileAsync(path);
            channels.AddRange(Limit(XmlTvGuide.Convert(tv), file.MaximumNumberOfChannels));
        }

        return channels;
    }
}
