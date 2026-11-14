namespace Prevue.Commands.Listings;

/// <summary>
/// The listings that ESQ has: the lineup and the programs of each broadcast day that were sent. It finds the commands
/// that change the listings of ESQ to new listings.
/// </summary>
/// <remarks>
/// ESQ ignores a lineup that is the same as its last lineup. If the lineup of a day changed, ESQ deletes the programs of
/// the day, so all the programs of the day must follow it. A new day, for example the next day after the day change at
/// 5:30 AM, also gets its lineup and all its programs. If the lineup did not change, only the new or changed programs
/// are sent. ESQ cannot delete one program, so a program that is not in the new listings stays.
/// </remarks>
public sealed class ListingsState
{
    private readonly Dictionary<DateOnly, byte[]> _lineUps = [];
    private readonly Dictionary<(DateOnly Day, int Slot, string Source), byte[]> _programs = [];

    /// <summary>The broadcast days that ESQ has listings for.</summary>
    public IEnumerable<DateOnly> Days => _lineUps.Keys.Order();

    /// <summary>Remembers the lineups and the programs of the commands that were sent. Other commands are ignored.</summary>
    public void Remember(IEnumerable<BaseCommand> commands)
    {
        foreach (var command in commands)
        {
            switch (command)
            {
                case ChannelLineUpCommand lineUp:
                    if (!_lineUps.TryGetValue(lineUp.Day, out var old) || !old.SequenceEqual(lineUp.Render()))
                        _programs.Keys.Where(k => k.Day == lineUp.Day).ToList().ForEach(k => _programs.Remove(k));
                    _lineUps[lineUp.Day] = lineUp.Render();
                    break;
                case ChannelProgramCommand program:
                    _programs[(program.Day, program.Slot, program.SourceName)] = program.Render();
                    break;
            }
        }
    }

    /// <summary>
    /// The commands of <paramref name="listings"/> that change the listings of ESQ, in the order of the listings. The
    /// state then has the new listings. The days before the first day of the new listings are forgotten.
    /// </summary>
    public List<BaseCommand> Changes(IReadOnlyList<BaseCommand> listings)
    {
        var newLineUps = listings.OfType<ChannelLineUpCommand>()
            .Where(c => !_lineUps.TryGetValue(c.Day, out var old) || !old.SequenceEqual(c.Render()))
            .Select(c => c.Day)
            .ToHashSet();

        var changes = new List<BaseCommand>();
        foreach (var command in listings)
        {
            var send = command switch
            {
                ChannelLineUpCommand lineUp => newLineUps.Contains(lineUp.Day),
                ChannelProgramCommand program => newLineUps.Contains(program.Day) ||
                                                 !_programs.TryGetValue((program.Day, program.Slot, program.SourceName),
                                                     out var old) || !old.SequenceEqual(program.Render()),
                _ => false
            };
            if (send)
                changes.Add(command);
        }

        Remember(changes);
        var days = listings.OfType<ChannelLineUpCommand>().Select(c => c.Day)
            .Concat(listings.OfType<ChannelProgramCommand>().Select(c => c.Day)).ToList();
        if (days.Count > 0)
            Forget(days.Min());
        return changes;
    }

    private void Forget(DateOnly firstDay)
    {
        foreach (var day in _lineUps.Keys.Where(d => d < firstDay).ToList())
            _lineUps.Remove(day);
        foreach (var key in _programs.Keys.Where(k => k.Day < firstDay).ToList())
            _programs.Remove(key);
    }
}
