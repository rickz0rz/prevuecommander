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
        var hashString =  Convert.ToHexString(shaM.ComputeHash(Encoding.ASCII.GetBytes(str)));
        return hashString.Length > 6 ? hashString[..6] : hashString;
    }

    private static DateTime ParseXmlTvDate(string date)
    {
        return DateTime.Parse(
            $"{date[0..4]}-{date[4..6]}-{date[6..8]}T{date[8..10]}:{date[10..12]}:{date[12..14]}{date[15..]}");
    }

    public static string ExtractChannelNumber(IEnumerable<string> displayNames)
    {
        foreach (var displayName in displayNames)
        {
            var components = displayName.Split(" ");
            foreach (var component in components)
            {
                if (component.ToCharArray().All(c => char.IsDigit(c) || c is '.' or '-'))
                {
                    return component;
                }
            }
        }

        throw new Exception("Unable to find channel number in displayName");
    }

    public static string ExtractCallSign(IEnumerable<string> displayNames)
    {
        foreach (var displayName in displayNames)
        {
            var components = displayName.Split(" ");
            foreach (var component in components)
            {
                if (component.ToCharArray().Any(char.IsLetter) && !component.Contains(":"))
                {
                    return component;
                }
            }
        }

        throw new Exception("Unable to find channel number in displayName");

        return displayNames.First(dn => !int.TryParse(dn, out _) && !dn.Contains(" "));
    }

    public static async Task<List<BaseCommand>> ImportXml(DateTime date, string xmlTvFilename,
        bool sendChannelLineUp = true, int maximumNumberOfChannels = int.MaxValue)
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
        var knownCallSigns = new List<string>();

        var numberOfChannelsToFetch = (xmlTvData.Channel.Count > maximumNumberOfChannels)
            ? maximumNumberOfChannels
            : xmlTvData.Channel.Count;

        for (var i = 0; i < numberOfChannelsToFetch; i++)
        {
            // Don't add duplicate call signs.
            var xmlTvChannel = xmlTvData.Channel.ElementAt(i);
            var callSign = ExtractCallSign(xmlTvChannel.Displayname);

            if (knownCallSigns.Contains(callSign))
                continue;

            knownCallSigns.Add(callSign);

            var commandChannel = new CommandChannel
            {
                CallSign = callSign,
                ChannelNumber = ExtractChannelNumber(xmlTvChannel.Displayname),
                SourceName = HashStringForSourceName(xmlTvChannel.Id)
            };
            channelsToAdd.Add(commandChannel);

            knownSources.Add(xmlTvChannel.Id);
        }

        if (sendChannelLineUp)
        {
            commands.Add(new ChannelLineUpCommand(date, channelsToAdd.ToArray()));
        }

        foreach (var programme in xmlTvData.Programme)
        {
            if (!knownSources.Contains(programme.Channel))
                continue;

            var sourceName = HashStringForSourceName(programme.Channel);
            var parsedDate = ParseXmlTvDate(programme.Start).AddHours(-1);

            // Find the current show in progress.
            // Also, find programming up to the next half-hour after the last visible
            // time block (so we can determine if we need to show >> or not)

            if (parsedDate < date.AddHours(2)) // 24
            {
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
