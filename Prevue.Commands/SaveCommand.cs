namespace Prevue.Commands;

/// <summary>
/// The '%' command: ESQ saves the listings, the local ads, the daylight saving time window and the other received
/// data to its drive at the next box off command. Without it, the data stays only in memory.
/// </summary>
public class SaveCommand() : BaseCommand('%')
{
    protected override byte[] GetPayload() => [];
}
