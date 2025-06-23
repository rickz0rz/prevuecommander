using System.Text.Json;
using Prevue.Commands;
using Prevue.Commands.Model;

namespace PrevueCommander.ChannelsDVR;

public static class ChannelsCore
{
    public static ChannelProgramCommand? GenerateProgramCommand(DateTime date,
        IEnumerable<Channel> knownSources, JsonElement channelElement, JsonElement airingElement)
    {
        try
        {
            // var sourceName = channelElement.GetProperty("Station").ToString();
            var sourceName = channelElement.GetProperty("CallSign").ToString();
            if (sourceName.EndsWith("HD")) sourceName = sourceName.Substring(0, sourceName.Length - "HD".Length);

            if (knownSources.All(knownSource => knownSource.SourceName != sourceName))
                return null;

            var parsedDate = DateTimeOffset.FromUnixTimeSeconds(airingElement.GetProperty("Time").GetInt64())
                .LocalDateTime
                .AddHours(-1);

            // Find the current show in progress.
            // Also, find programming up to the next half-hour after the last visible
            // time block (so we can determine if we need to show >> or not)

            // Expand this to not include shows more than 2 hours ago
            // that aren't in progress to potentially reduce transmission size?
            if (parsedDate >= date.AddHours(2))
                return null;

            var titleValue = airingElement.GetProperty("Title").GetString();
            var isMovie = airingElement.TryGetProperty("MovieID", out _);
            var title = isMovie
                ? titleValue.Split("\"", StringSplitOptions.RemoveEmptyEntries).First().Split("(").First().Trim()
                    .Replace("%", "%%")
                : titleValue.Replace("%", "%%");
            var summary = airingElement.TryGetProperty("Summary", out _)
                ? airingElement.GetProperty("Summary").GetString().Replace("%", "%%")
                : string.Empty;
            var foundRating = airingElement.TryGetProperty("ContentRating", out _)
                ? airingElement.GetProperty("ContentRating").GetString()
                : string.Empty;
            var rating = !string.IsNullOrWhiteSpace(foundRating) ? $" %{foundRating.Replace("-", "")}%" : "";
            var stereo = airingElement.GetProperty("Tags").EnumerateArray().Any(x => x.GetString().Equals("Stereo"))
                ? "%STEREO%"
                : string.Empty;
            var closedCaptioning =
                airingElement.GetProperty("Tags").EnumerateArray().Any(x => x.GetString().Equals("CC"))
                    ? "%CC%"
                    : string.Empty;
            var movieReleaseYear = isMovie
                ? airingElement.GetProperty("ReleaseYear").GetInt32().ToString()
                : string.Empty;
            var extraString = isMovie
                ? $"{summary}{rating}{stereo}{closedCaptioning}"
                : $"{rating}{stereo}{closedCaptioning}".Trim();
            extraString = string.IsNullOrWhiteSpace(extraString) ? string.Empty : $" {extraString}".TrimEnd();
            var generatedDescription = isMovie
                ? $"\"{title.Trim()}\" ({movieReleaseYear}){extraString}"
                : $"{title.Trim()}{extraString}";

            return new ChannelProgramCommand(parsedDate,
                sourceName,
                isMovie,
                generatedDescription
            );
        }
        catch (Exception e)
        {
            return null;
        }
    }
}