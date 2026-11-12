using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// Sends a new <c>DF0:LOGO.LST</c>. Prevue Commander cannot read the list of the drive, so give all the lines to keep,
/// for example <c>Logos/tvgsport.uv,</c>. A line that ends with a comma always shows. A line without a comma is a
/// channel logo.
/// </summary>
/// <remarks>
/// ESQ reads LOGO.LST when it starts. With <see cref="Reload"/>, ESQ reads it at once. The reload needs
/// <c>cleanUpUnusedLogos: 'N'</c> in the configuration, because the logo scan stops ESQ when DH2:logos does not exist.
/// </remarks>
public record LogoListPlaybookCommand : SimplePlaybookCommand
{
    public List<string> Lines { get; init; } = [];

    /// <summary>True tells ESQ to read the new list at once (the utility action ReloadLogoList).</summary>
    public bool Reload { get; init; }

    public string Address { get; init; } = "*";

    protected override IReadOnlyList<BaseCommand> Transform(PlaybookContext context)
    {
        var bad = Lines.FirstOrDefault(line => line.Length == 0 || line.Any(c => c is < ' ' or > '~'));
        if (bad != null)
            throw new ArgumentException($"Each line of LOGO.LST must be printable ASCII text: \"{bad}\".");
        var commands = FileTransferPlaybookCommand.Commands("DF0:LOGO.LST",
            Pictures.Latin1(string.Concat(Lines.Select(line => line + "\r\n"))), Address).ToList();
        if (Reload)
            commands.Add(new UtilityCommand(UtilityAction.ReloadLogoList));
        return commands;
    }
}
