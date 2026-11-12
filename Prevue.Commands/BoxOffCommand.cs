namespace Prevue.Commands;

/// <summary>
/// The box off command ($BB): ESQ stops accepting commands until the next 'A' command. If a save command came
/// before it, ESQ first saves its data to its drive.
/// </summary>
public class BoxOffCommand() : BaseCommand(0xBB)
{
    protected override byte[] GetPayload() => [0xBB];
}
