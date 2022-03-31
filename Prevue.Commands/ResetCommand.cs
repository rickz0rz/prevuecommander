namespace Prevue.Commands;

public class ResetCommand : BaseCommand
{
    public ResetCommand() : base('R')
    {
    }

    public override string ToString()
    {
        return nameof(ResetCommand);
    }

    protected override byte[] GetMessageBytes()
    {
        return Array.Empty<byte>();
    }
}
