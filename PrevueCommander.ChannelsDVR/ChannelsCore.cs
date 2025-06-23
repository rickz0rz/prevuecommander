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
            var sourceName = channelElement.GetProperty("Station").ToString();

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

            var title = airingElement.GetProperty("Title").GetString().Replace("%", "%%");
            var summary = airingElement.TryGetProperty("Summary", out var _)
                ? airingElement.GetProperty("Summary").GetString().Replace("%", "%%")
                : string.Empty;
            var isMovie = airingElement.TryGetProperty("MovieID", out var _);
            var foundRating = airingElement.TryGetProperty("ContentRating", out var _)
                ? airingElement.GetProperty("ContentRating").GetString()
                : string.Empty;
            var rating = !string.IsNullOrWhiteSpace(foundRating) ? $" %{foundRating.Replace("-", "")}%" : "";
            var stereo = string.Empty;
            var closedCaptioning = string.Empty;
            var movieReleaseYear = isMovie
                ? airingElement.GetProperty("ReleaseYear").GetInt32().ToString()
                : string.Empty;
            var generatedDescription = isMovie
                ? $"\"{title}\" ({movieReleaseYear}) {summary}{rating}{stereo}{closedCaptioning}"
                : $"{title}{rating}{stereo}{closedCaptioning}";

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
