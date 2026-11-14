using System.Globalization;
using System.IO.Compression;
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

    /// <summary>Reads an XMLTV file. The file can be compressed with gzip.</summary>
    public static async Task<Tv> ParseFileAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        return await ParseAsync(stream, path, cancellationToken);
    }

    /// <summary>
    /// Downloads an XMLTV file from an HTTP or HTTPS address. The file can be compressed with gzip.
    /// </summary>
    public static async Task<Tv> DownloadAsync(HttpClient http, Uri address, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"The XMLTV address {address} gave the status {(int)response.StatusCode} {response.ReasonPhrase}.");

        // Copy the data to memory, because the gzip check reads the first bytes again.
        var data = new MemoryStream();
        await using (var body = await response.Content.ReadAsStreamAsync(cancellationToken))
            await body.CopyToAsync(data, cancellationToken);
        data.Position = 0;
        return await ParseAsync(data, address.ToString(), cancellationToken);
    }

    /// <summary>Reads XMLTV data from a stream that can seek. Data that starts with the gzip signature is decompressed.</summary>
    /// <param name="name">The file name or the address, for the error messages.</param>
    public static async Task<Tv> ParseAsync(Stream stream, string name, CancellationToken cancellationToken)
    {
        var signature = new byte[2];
        var count = await stream.ReadAtLeastAsync(signature, 2, false, cancellationToken);
        stream.Position = 0;
        var input = count == 2 && signature is [0x1F, 0x8B]
            ? new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true)
            : stream;

        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
            using var reader = XmlReader.Create(input, settings);
            return (Tv?)new XmlSerializer(typeof(Tv)).Deserialize(reader)
                   ?? throw new InvalidDataException($"{name} is not an XMLTV file.");
        }
        catch (InvalidOperationException e)
        {
            throw new InvalidDataException($"{name} is not an XMLTV file: {e.InnerException?.Message ?? e.Message}", e);
        }
        finally
        {
            if (input != stream)
                await input.DisposeAsync();
        }
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
                programmes[c.Id!].Select(Program).OfType<GuideProgram>().ToList(), LogoUrl: c.Icon?.Src));
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
        return new GuideProgram(start, end, text, isMovie, IsStereo(programme.Audio?.Stereo));
    }

    /// <summary>
    /// True for an XMLTV stereo value of stereo sound: "stereo", "dolby", "dolby digital", "surround" or "bilingual".
    /// "mono" and no value are not stereo.
    /// </summary>
    public static bool IsStereo(string? value)
    {
        var text = value?.Trim();
        return !string.IsNullOrEmpty(text) && !text.Equals("mono", StringComparison.OrdinalIgnoreCase);
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
