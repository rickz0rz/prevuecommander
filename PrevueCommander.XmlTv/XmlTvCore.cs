using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Prevue.Commands;
using PrevueCommander.XmlTv.Model;
using CommandChannel = Prevue.Commands.Model.Channel;

namespace PrevueCommander.XmlTv;

public static class XmlTvCore
{
    private static string HashStringForSourceName(string str)
    {
        var shaM = SHA512.Create();
        return Convert.ToHexString(shaM.ComputeHash(Encoding.ASCII.GetBytes(str)));
    }

    private static DateTime ParseXmlTvDate(string date)
    {
        return DateTime.Parse(
            $"{date[0..4]}-{date[4..6]}-{date[6..8]}T{date[8..10]}:{date[10..12]}:{date[12..14]}{date[15..]}");
    }

    public static async Task<List<BaseCommand>> ImportXml(DateTime date, string xmlTvFilename,
        int maximumNumberOfChannels = int.MaxValue)
    {
        var commands = new List<BaseCommand>();

        var xmlReaderSettings = new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Ignore
        };
        await using var fileStream = new FileStream(xmlTvFilename, FileMode.Open);
        using var xmlReader = XmlReader.Create(fileStream, xmlReaderSettings);
        var xmlTvData = (Tv)(new XmlSerializer(typeof(Tv)).Deserialize(xmlReader));

        var channelsToAdd = new List<Prevue.Commands.Model.Channel>();
        var knownSources = new List<string>();

        for (var i = 0; i < maximumNumberOfChannels; i++)
        {
            var xmlTvChannel = xmlTvData.Channel.ElementAt(i);
            var commandChannel = new CommandChannel
            {
                CallSign = xmlTvChannel.Displayname.First(dn => !int.TryParse(dn, out _) && !dn.Contains(" ")),
                ChannelNumber = xmlTvChannel.Displayname.First(dn => int.TryParse(dn, out _)),
                SourceName = HashStringForSourceName(xmlTvChannel.Id)[..6]
            };
            channelsToAdd.Add(commandChannel);
            knownSources.Add(xmlTvChannel.Id);
        }

        commands.Add(new ChannelLineUpCommand(date, channelsToAdd.ToArray()));

        foreach (var programme in xmlTvData.Programme)
        {
            if (!knownSources.Contains(programme.Channel))
                continue;

            var sourceName = HashStringForSourceName(programme.Channel)[..6];
            var parsedDate = ParseXmlTvDate(programme.Start).AddHours(-1);

            if (parsedDate < date.AddHours(2)) // 24
            {
                var title = programme.Title.First(t => t.Lang == "en").Text;
                var desc = programme.Desc.FirstOrDefault(d => d.Lang == "en")?.Text;
                var isMovie = programme.Category.Any(x => x.Lang == "en" && x.Text == "Movie") &&
                              !string.IsNullOrWhiteSpace(desc);
                var closedCaptioning = programme.Subtitles.Any() ? " %CC%" : string.Empty;
                var generatedDescription = isMovie
                    ? $"\"{title}\" {desc} ({programme.Date}){closedCaptioning}"
                    : $"{title}{closedCaptioning}";

                commands.Add(new ChannelProgramCommand(parsedDate,
                    sourceName,
                    isMovie,
                    generatedDescription
                ));
            }
        }

        return commands;
    }
}
