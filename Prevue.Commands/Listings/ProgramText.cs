using System.Text.RegularExpressions;

namespace Prevue.Commands.Listings;

/// <summary>
/// Makes the text of a program in the format of the Prevue data feed. The format is the same as the format of the
/// listings tool of Esquire (<c>GuideConverter</c>).
/// </summary>
public static partial class ProgramText
{
    /// <summary>The ratings that ESQ changes to a symbol of its font, when they are in parentheses.</summary>
    private static readonly HashSet<string> Ratings =
    [
        "R", "Adult", "PG", "NR", "PG-13", "G", "NC-17", "TV-Y", "TV-Y7", "TV-PG", "TV-G", "TV-M", "TV-MA", "TV-14"
    ];

    /// <summary>
    /// The text of a program that is not a movie: the title and the closed caption symbol, for example
    /// <c>Jeopardy! |</c>. A sports event adds its event title, for example
    /// <c>NFL Football: Detroit Lions at Carolina Panthers</c>.
    /// </summary>
    public static string Show(string title, bool closedCaptions = false, string? eventTitle = null)
    {
        var text = Clean(title);
        if (!string.IsNullOrWhiteSpace(eventTitle))
            text = $"{text}: {Clean(eventTitle)}";
        return Join(text, closedCaptions ? "|" : null);
    }

    /// <summary>
    /// The text of a movie: the title in quotation marks, the year, the summary, the rating and the closed caption
    /// symbol, for example <c>"Casablanca" (1942) A cafe owner meets an old love. (PG) |</c>. ESQ shows the text up
    /// to the second quotation mark on the first line, so quotation marks in the title become apostrophes.
    /// </summary>
    public static string Movie(string title, int? year = null, string? summary = null, string? rating = null,
        bool closedCaptions = false)
    {
        var yearInTitle = YearSuffix().Match(title);
        if (yearInTitle.Success)
        {
            title = title[..yearInTitle.Index];
            if (year is not > 0)
                year = int.Parse(yearInTitle.Groups[1].Value);
        }

        return Join($"\"{Clean(title).Replace('"', '\'')}\"", year is > 0 ? $"({year})" : null, Clean(summary),
            RatingToken(rating), closedCaptions ? "|" : null);
    }

    /// <summary>
    /// The rating in the form that ESQ changes to a symbol, for example "(TV-PG)" for "TVPG" or "TV-PG", or null if
    /// ESQ has no symbol for it.
    /// </summary>
    public static string? RatingToken(string? rating)
    {
        if (string.IsNullOrWhiteSpace(rating))
            return null;

        var compact = rating.Trim().Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        var match = Ratings.FirstOrDefault(r => r.Replace("-", string.Empty).ToUpperInvariant() == compact);
        return match == null ? null : $"({match})";
    }

    /// <summary>"|" is the closed caption symbol in the grid, so it becomes "/" in the text of the guide.</summary>
    private static string Clean(string? text) => (text ?? string.Empty).Trim().Replace('|', '/');

    private static string Join(params string?[] parts) =>
        string.Join(' ', parts.Where(part => !string.IsNullOrWhiteSpace(part)));

    [GeneratedRegex(@"\s*\((\d{4})\)\s*$")]
    private static partial Regex YearSuffix();
}
