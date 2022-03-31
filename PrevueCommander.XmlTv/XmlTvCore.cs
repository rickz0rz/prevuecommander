using System.Xml;
using System.Xml.Serialization;
using Prevue.Commands;
using PrevueCommander.XmlTv.Model;
using CommandChannel = Prevue.Commands.Model.Channel;

namespace PrevueCommander.XmlTv;

public static class XmlTvCore
{
    private static DateTime ParseXmlTvDate(string date)
    {
        return DateTime.Parse(
            $"{date[0..4]}-{date[4..6]}-{date[6..8]}T{date[8..10]}:{date[10..12]}:{date[12..14]}{date[15..]}");
    }

    public static async Task<Tv> ParseXmlFile(string xmlTvFilename)
    {
        var xmlReaderSettings = new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Ignore
        };
        await using var fileStream = new FileStream(xmlTvFilename, FileMode.Open);
        using var xmlReader = XmlReader.Create(fileStream, xmlReaderSettings);
        return (Tv)new XmlSerializer(typeof(Tv)).Deserialize(xmlReader)!;
    }

    public static ChannelProgramCommand? GenerateProgramCommand(DateTime date,
        IEnumerable<string> knownSources, Programme programme)
    {
        if (!knownSources.Contains(programme.SourceName))
            return null;

        var sourceName = programme.SourceName;
        var parsedDate = ParseXmlTvDate(programme.Start).AddHours(-1);

        // Find the current show in progress.
        // Also, find programming up to the next half-hour after the last visible
        // time block (so we can determine if we need to show >> or not)

        if (parsedDate >= date.AddHours(2))
            return null;

        var title = programme.Title.FirstOrDefault(t => t.Lang.Split(",")
                        .Any(l => l.Trim().Equals("en", StringComparison.OrdinalIgnoreCase)))?.Text ??
                    programme.Title.FirstOrDefault()?.Text ?? "No Data";
        var desc = programme.Desc.FirstOrDefault(t => t.Lang.Split(",")
                       .Any(l => l.Trim().Equals("en", StringComparison.OrdinalIgnoreCase)))?.Text ??
                   programme.Desc.FirstOrDefault()?.Text ?? "No Data";
        var isMovie = programme.Category.Any(x =>
                          x.Lang.Split(",").Any(y =>
                              y.Trim().Equals("en", StringComparison.OrdinalIgnoreCase)) &&
                          x.Text == "Movie") &&
                      !string.IsNullOrWhiteSpace(desc);
        var closedCaptioning = programme.Subtitles.Any() ? " %CC%" : string.Empty;
        var generatedDescription = isMovie
            ? $"\"{title}\" ({programme.Date}) {desc} {closedCaptioning}"
            : $"{title}{closedCaptioning}";

        return new ChannelProgramCommand(parsedDate,
            sourceName,
            isMovie,
            generatedDescription
        );
    }
}
