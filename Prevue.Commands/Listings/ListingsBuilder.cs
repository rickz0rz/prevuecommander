using System.Globalization;
using Prevue.Commands.Model;
using Prevue.Core;

namespace Prevue.Commands.Listings;

/// <summary>
/// Converts the channels of a guide to the 'C' and 'P' commands for the current and the next broadcast day of ESQ.
/// </summary>
public static class ListingsBuilder
{
    /// <summary>
    /// Makes the commands for the two broadcast days that ESQ keeps at the local time <paramref name="now"/>. Each day
    /// gets its lineup (if <paramref name="sendChannelLineUp"/> is true) and the programs of its 48 slots.
    /// </summary>
    /// <remarks>
    /// A program goes into the slot in which it starts. A program that starts before the day and continues into it
    /// goes into slot 1. If a program does not start at :00 or :30, its text starts with the time, for example
    /// "( 8:15) News", as in the Prevue data feed. If two programs start in one slot, the grid shows the first.
    /// </remarks>
    /// <param name="characters">The characters that the program text can have.</param>
    /// <param name="stereo">
    /// True to mark all the channels as stereo channels, and to add "In Stereo" to the programs with stereo sound. ESQ
    /// shows the stereo symbol for it.
    /// </param>
    public static List<BaseCommand> Build(IEnumerable<GuideChannel> guideChannels, DateTime now,
        bool sendChannelLineUp = true, CharacterSet characters = CharacterSet.Latin1, bool stereo = false)
    {
        var channels = guideChannels.Take(ChannelLineUpCommand.MaximumChannels).ToList();
        var sourceNames = SourceNames(channels.Select(c => c.CallSign));
        var lineUp = channels
            .Select((c, i) => new Channel(sourceNames[i], c.Number, c.CallSign, c.Premium, stereo))
            .ToList();

        var commands = new List<BaseCommand>();
        var today = BroadcastDay.Current(now);
        foreach (var day in new[] { today, today.AddDays(1) })
        {
            if (sendChannelLineUp)
                commands.Add(new ChannelLineUpCommand(day, lineUp));

            for (var i = 0; i < channels.Count; i++)
                commands.AddRange(ProgramCommands(day, sourceNames[i], channels[i].Programs, characters, stereo));
        }

        return commands;
    }

    private static IEnumerable<ChannelProgramCommand> ProgramCommands(DateOnly day, string sourceName,
        IEnumerable<GuideProgram> programs, CharacterSet characters, bool stereo)
    {
        var dayStart = BroadcastDay.StartOf(day);
        var dayEnd = BroadcastDay.StartOf(day.AddDays(1));
        var usedSlots = new HashSet<int>();
        foreach (var program in programs.OrderBy(p => p.Start))
        {
            if (program.End <= dayStart || program.Start >= dayEnd)
                continue;

            var slot = BroadcastDay.Slot(day, program.Start);
            if (!usedSlots.Add(slot))
                continue;

            var text = program.Start > dayStart && program.Start.Minute % 30 != 0
                ? $"({program.Start.ToString("%h", CultureInfo.InvariantCulture),2}:{program.Start:mm}) {program.Text}"
                : program.Text;
            if (stereo && program.IsStereo)
            {
                // Convert the text first, so that the 120-character limit does not cut the tag.
                text = ProgramText.AddStereo(PrevueText.ToPrintable(text, characters),
                    ChannelProgramCommand.MaximumTextLength);
            }

            yield return new ChannelProgramCommand(day, slot, sourceName, program.IsMovie, text, characters);
        }
    }

    /// <summary>
    /// Gives each channel a source name of 1 to 6 letters or digits, from its call sign. The names are different,
    /// because a 'P' command finds its channel by the source name.
    /// </summary>
    public static List<string> SourceNames(IEnumerable<string> callSigns)
    {
        var used = new HashSet<string>();
        var names = new List<string>();
        foreach (var callSign in callSigns)
        {
            var name = new string(callSign.Where(char.IsAsciiLetterOrDigit).Take(Channel.MaximumSourceNameLength)
                .ToArray()).ToUpperInvariant();
            if (name.Length == 0)
                name = "CH";

            var candidate = name;
            for (var n = 2; !used.Add(candidate); n++)
            {
                var suffix = n.ToString();
                candidate = name[..Math.Min(name.Length, Channel.MaximumSourceNameLength - suffix.Length)] + suffix;
            }

            names.Add(candidate);
        }

        return names;
    }
}
