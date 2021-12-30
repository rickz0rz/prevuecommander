namespace Prevue.Commands;

public class ResetCommand : BaseCommand
{
    public ResetCommand() : base((byte)'R')
    {
    }

    protected override byte[] GetMessageBytes()
    {
        return Array.Empty<byte>();
    }
}