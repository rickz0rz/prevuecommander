namespace Prevue.Commands;

/// <summary>
/// The 'R' command: ESQ shows "Reset command received" and reboots the Amiga about 10 seconds later. ESQ accepts it
/// only directly after a matching 'A' command.
/// </summary>
public class ResetCommand() : BaseCommand('R')
{
    protected override byte[] GetPayload() => [];
}
