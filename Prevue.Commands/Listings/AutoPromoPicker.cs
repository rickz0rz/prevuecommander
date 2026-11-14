using System.Text.RegularExpressions;
using Prevue.Commands.Model;
using Prevue.Core;

namespace Prevue.Commands.Listings;

/// <summary>The rules that choose the program of an automatic promo. The rules are those of the Esquire launcher.</summary>
/// <param name="Movies">True for movies only, false for no movies, null for all programs.</param>
/// <param name="Premium">True for premium channels only, false for no premium channels, null for all channels.</param>
/// <param name="Channels">Call letters (with '*' for any text) or channel numbers. Empty for all channels.</param>
/// <param name="Titles">Parts of titles. Upper case and lower case are the same. Empty for all titles.</param>
/// <param name="WithinHours">The hours after the current half hour for the start of the program.</param>
/// <param name="IncludeNow">True to also choose a program that starts in the current half hour.</param>
/// <param name="Repeat">The number of the last automatic promos whose titles do not show again.</param>
/// <param name="Random">True to choose by chance, false to choose the next program.</param>
public sealed record AutoPromoCriteria(
    bool? Movies = null,
    bool? Premium = null,
    IReadOnlyList<string>? Channels = null,
    IReadOnlyList<string>? Titles = null,
    double WithinHours = 3,
    bool IncludeNow = false,
    int Repeat = 10,
    bool Random = false);

/// <summary>A program that an automatic promo can show.</summary>
/// <param name="Title">The title, for example <c>Casablanca</c>.</param>
/// <param name="Search">The text that finds the program in a promo. A movie title is in quotation marks.</param>
/// <param name="CallLetters">The call letters as the lineup sends them.</param>
/// <param name="Number">The channel number.</param>
/// <param name="Start">The start of the half hour of the program.</param>
public sealed record PromoProgram(
    string Title,
    string Search,
    string CallLetters,
    string Number,
    DateTime Start,
    bool IsMovie,
    bool Premium);

/// <summary>
/// Chooses the programs of automatic promos from the listings that ESQ has. The text of a promo comes from the text of
/// the listings, so ESQ always finds the program.
/// </summary>
/// <remarks>
/// The picker chooses the first program that fits the criteria, in the order of the time and of the lineup (or in a
/// random order), whose title was not in one of the last <see cref="AutoPromoCriteria.Repeat"/> promos. If all the
/// titles were in these promos, it chooses the title of the oldest promo.
/// </remarks>
public sealed partial class AutoPromoPicker
{
    private readonly List<string> _recent = [];
    private readonly Lock _lock = new();

    /// <summary>The programs of the listings that fit the criteria at the local time, in the order of the choice.</summary>
    public static List<PromoProgram> Candidates(IEnumerable<BaseCommand> listings, DateTime now,
        AutoPromoCriteria criteria)
    {
        // The last lineup of a day and the last program of a slot are the ones that ESQ has.
        var commands = listings.ToList();
        var lineUps = new Dictionary<DateOnly, List<Channel>>();
        foreach (var lineUp in commands.OfType<ChannelLineUpCommand>())
            lineUps[lineUp.Day] = lineUp.Channels.ToList();
        var programs = new Dictionary<(DateOnly, int, string), ChannelProgramCommand>();
        foreach (var program in commands.OfType<ChannelProgramCommand>())
            programs[(program.Day, program.Slot, program.SourceName)] = program;

        var currentSlot = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute >= 30 ? 30 : 0, 0);
        var first = criteria.IncludeNow ? currentSlot : currentSlot.AddMinutes(30);
        var last = currentSlot.AddHours(criteria.WithinHours);

        var candidates = new List<(PromoProgram Program, int Channel)>();
        foreach (var program in programs.Values)
        {
            var start = BroadcastDay.StartOf(program.Day).AddMinutes((program.Slot - 1) * 30);
            if (start < first || start > last || !lineUps.TryGetValue(program.Day, out var channels))
                continue;

            var index = channels.FindIndex(c => c.SourceName == program.SourceName);
            if (index < 0)
                continue;

            var channel = channels[index];
            var (title, search) = Titles(program.Text);
            var callLetters = Limit(channel.CallSign, Channel.MaximumCallSignLength);
            var number = channel.ChannelNumber ?? string.Empty;
            var candidate = new PromoProgram(title, search, callLetters, number, start, program.IsMovie,
                channel.Premium);
            if (title.Length > 0 && Matches(candidate, criteria))
                candidates.Add((candidate, index));
        }

        return candidates.OrderBy(c => c.Program.Start).ThenBy(c => c.Channel).Select(c => c.Program).ToList();
    }

    /// <summary>Chooses a program, or returns null if no program fits the criteria.</summary>
    public PromoProgram? Pick(IEnumerable<BaseCommand> listings, DateTime now, AutoPromoCriteria criteria)
    {
        var candidates = Candidates(listings, now, criteria);
        if (criteria.Random)
            candidates = candidates.OrderBy(_ => System.Random.Shared.Next()).ToList();
        if (candidates.Count == 0)
            return null;

        lock (_lock)
        {
            var recent = _recent.TakeLast(criteria.Repeat).ToList();
            var pick = candidates.FirstOrDefault(p => !recent.Contains(Key(p))) ??
                       candidates.OrderBy(p => _recent.IndexOf(Key(p))).First();
            _recent.Remove(Key(pick));
            _recent.Add(Key(pick));
            if (_recent.Count > 100)
                _recent.RemoveAt(0);
            return pick;
        }
    }

    /// <summary>
    /// The title of a program text, and the text that finds it in a promo. The start time, for example "( 8:15) ",
    /// the stereo tag and the text from "|" are not part of the title. A movie title is in quotation marks, and its
    /// search text keeps them, so that the promo finds only that title.
    /// </summary>
    public static (string Title, string Search) Titles(string text)
    {
        text = StartTime().Replace(text, string.Empty);
        if (text.StartsWith('"') && text.IndexOf('"', 1) is var end and > 1)
        {
            var quoted = text[1..end];
            return (quoted, $"\"{quoted}\"");
        }

        var bar = text.IndexOf('|');
        if (bar >= 0)
            text = text[..bar];
        var title = text.Replace(ProgramText.StereoTag, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return (title, title);
    }

    private static bool Matches(PromoProgram program, AutoPromoCriteria criteria)
    {
        if (criteria.Movies is { } movies && program.IsMovie != movies)
            return false;
        if (criteria.Premium is { } premium && program.Premium != premium)
            return false;
        if (criteria.Channels is { Count: > 0 } channels && !channels.Any(channel =>
                string.Equals(channel, program.Number, StringComparison.OrdinalIgnoreCase) ||
                Regex.IsMatch(program.CallLetters, "^" + Regex.Escape(channel).Replace("\\*", ".*") + "$",
                    RegexOptions.IgnoreCase)))
            return false;
        return criteria.Titles is not { Count: > 0 } titles ||
               titles.Any(title => program.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
    }

    private static string Key(PromoProgram program) => program.Search.ToUpperInvariant();

    private static string Limit(string? text, int length) =>
        string.IsNullOrEmpty(text) ? string.Empty : text.Length > length ? text[..length] : text;

    [GeneratedRegex(@"^\(\s?\d{1,2}:\d{2}\)\s*")]
    private static partial Regex StartTime();
}
