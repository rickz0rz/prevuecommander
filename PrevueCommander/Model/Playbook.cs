using PrevueCommander.Model.PlaybookCommands;

namespace PrevueCommander.Model;

/// <summary>The connection settings and the list of commands to send.</summary>
public record Playbook
{
    public Configuration Configuration { get; init; } = new();

    public List<IPlaybookCommand> Commands { get; init; } = [];

    /// <summary>The automatic promos after the commands, or null for none.</summary>
    public AutoPromoSettings? AutoPromo { get; init; }
}
