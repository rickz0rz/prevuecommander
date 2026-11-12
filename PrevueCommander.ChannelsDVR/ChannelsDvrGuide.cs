using Prevue.Commands.Listings;

namespace PrevueCommander.ChannelsDVR;

/// <summary>
/// Converts the guide of a Channels DVR server to the channels and the programs for Prevue. The text of the programs
/// is the same as the text that the listings tool of Esquire makes from the same guide.
/// </summary>
public static class ChannelsDvrGuide
{
    /// <summary>
    /// The channels of the guide that have a station, are not hidden and are not Music Choice channels (if
    /// <paramref name="excludeMusicChoice"/> is true), without duplicate numbers or call signs. With
    /// <paramref name="onlyHD"/>, only the HD channels.
    /// </summary>
    public static IEnumerable<GuideChannel> Convert(IEnumerable<ChannelsDvrGuideEntry> guide, bool onlyHD,
        bool excludeMusicChoice)
    {
        return guide
            .Where(entry => entry.Channel is { Hidden: false, Station: not null } && (!onlyHD || entry.Channel.HD))
            .Where(entry => !excludeMusicChoice || !(entry.Channel.CallSign ?? string.Empty).StartsWith("MC"))
            .DistinctBy(entry => entry.Channel.Number)
            .DistinctBy(entry => CallSign(entry.Channel))
            .Select(entry => new GuideChannel(entry.Channel.Number, CallSign(entry.Channel),
                (entry.Airings ?? []).Select(Program).ToList(), LogoUrl: entry.Channel.Image));
    }

    private static string CallSign(ChannelsDvrChannel channel) =>
        string.IsNullOrWhiteSpace(channel.CallSign) ? channel.Name ?? channel.Number : channel.CallSign;

    private static GuideProgram Program(ChannelsDvrAiring airing)
    {
        var start = DateTimeOffset.FromUnixTimeSeconds(airing.Time).LocalDateTime;
        var end = start.AddSeconds(airing.Duration);
        var isMovie = airing.Categories?.Contains("Movie") == true;
        var closedCaptions = airing.Tags?.Contains("CC") == true;
        var title = airing.Title ?? string.Empty;
        var text = isMovie
            ? ProgramText.Movie(title, airing.ReleaseYear, airing.Summary, airing.ContentRating, closedCaptions)
            : ProgramText.Show(title, closedCaptions,
                airing.Categories?.Contains("Sports") == true ? airing.EventTitle : null);
        return new GuideProgram(start, end, text, isMovie);
    }
}
