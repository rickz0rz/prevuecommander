using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using Prevue.Commands.Listings;
using PrevueCommander.XmlTv.Model;

namespace PrevueCommander.XmlTv;

/// <summary>Reads an XMLTV file and converts it to the channels and the programs for Prevue.</summary>
public static class XmlTvGuide
{
    private static readonly string[] DateFormats = ["yyyyMMddHHmmss zzz", "yyyyMMddHHmmss", "yyyyMMddHHmm zzz",
        "yyyyMMddHHmm"];

    public static async Task<Tv> ParseFileAsync(string path)
    {
        var settings = new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Ignore };
        await using var stream = File.OpenRead(path);
        using var reader = XmlReader.Create(stream, settings);
        return (Tv?)new XmlSerializer(typeof(Tv)).Deserialize(reader)
               ?? throw new InvalidDataException($"The file {path} is not an XMLTV file.");
    }

    /// <summary>The channels of the file, in the order of the file, each with its programs.</summary>
    public static IEnumerable<GuideChannel> Convert(Tv tv)
    {
        var programmes = (tv.Programme ?? [])
            .Where(p => p.Channel != null)
            .ToLookup(p => p.Channel!);
        return (tv.Channel ?? [])
            .Where(c => c.Id != null)
            .Select(c => new GuideChannel(c.ChannelNumber, c.CallSign,
                programmes[c.Id!].Select(Program).OfType<GuideProgram>().ToList()));
    }

    /// <summary>A date of XMLTV, for example "20201101160000 +0000", as a local time.</summary>
    public static DateTime ParseDate(string date) =>
        DateTimeOffset.ParseExact(date.Trim(), DateFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal).LocalDateTime;

    private static GuideProgram? Program(Programme programme)
    {
        if (string.IsNullOrWhiteSpace(programme.Start))
            return null;

        var start = ParseDate(programme.Start);
        var end = string.IsNullOrWhiteSpace(programme.Stop) ? start.AddMinutes(30) : ParseDate(programme.Stop);
        var title = English(programme.Title?.Select(t => (t.Lang, t.Text))) ?? "No Data";
        var description = English(programme.Desc?.Select(d => (d.Lang, d.Text)));
        var rating = programme.Rating?.SelectMany(r => r.Value ?? []).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        var closedCaptions = programme.Subtitles?.Count > 0;
        var isMovie = programme.Category?.Any(c => c.Text?.Trim() == "Movie" && IsEnglish(c.Lang)) == true;
        int? year = int.TryParse(programme.Date?.Length >= 4 ? programme.Date[..4] : null, out var y) ? y : null;

        var text = isMovie
            ? ProgramText.Movie(title, year, description, rating, closedCaptions)
            : ProgramText.Show(title, closedCaptions);
        return new GuideProgram(start, end, text, isMovie);
    }

    /// <summary>The English text, or the first text if no text is in English.</summary>
    private static string? English(IEnumerable<(string? Lang, string? Text)>? texts)
    {
        var list = texts?.Where(t => !string.IsNullOrWhiteSpace(t.Text)).ToList() ?? [];
        return (list.FirstOrDefault(t => IsEnglish(t.Lang)).Text ?? list.FirstOrDefault().Text)?.Trim();
    }

    private static bool IsEnglish(string? lang) =>
        string.IsNullOrWhiteSpace(lang) ||
        lang.Split(',').Any(l => l.Trim().StartsWith("en", StringComparison.OrdinalIgnoreCase));
}
