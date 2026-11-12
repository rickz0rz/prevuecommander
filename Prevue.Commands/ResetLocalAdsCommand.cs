namespace Prevue.Commands;

/// <summary>
/// The 'L' command with the code $92: ESQ deletes all its local ads. ESQ accepts it only after an 'F' command, and
/// only when the local text ad source of the 'f' configuration is 'R' or 'S'.
/// </summary>
public class ResetLocalAdsCommand() : BaseCommand('L')
{
    protected override byte[] GetPayload() => [0x92];
}
