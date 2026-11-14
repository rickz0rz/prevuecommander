using Prevue.Commands;

namespace PrevueCommander;

/// <summary>
/// The session commands of a playbook: its address command, its last status packet, and a save command if it has one.
/// The watch mode and the web server put the data line commands of an update or a request in such a session.
/// </summary>
public sealed class SessionTemplate
{
    private AddressCommand _address = new("*");
    private ConfigurationCommand _status = new();
    private bool _save;

    /// <summary>Remembers the session commands of the commands that were sent. The defaults stay for a missing command.</summary>
    public void Remember(IEnumerable<BaseCommand> sent)
    {
        var commands = sent.ToList();
        _address = commands.OfType<AddressCommand>().FirstOrDefault() ?? _address;
        _status = commands.OfType<ConfigurationCommand>().LastOrDefault() ?? _status;
        _save |= commands.OfType<SaveCommand>().Any();
    }

    /// <summary>
    /// The commands in a session: the address command, the status packet, the commands, a save command if the
    /// playbook has one, and a box off command.
    /// </summary>
    public List<BaseCommand> Wrap(IEnumerable<BaseCommand> commands)
    {
        List<BaseCommand> session = [_address, _status, .. commands];
        if (_save)
            session.Add(new SaveCommand());
        session.Add(new BoxOffCommand());
        return session;
    }
}
