namespace Prevue.Commands;

public class LocalAdResetCommand : BaseCommand
{
    public LocalAdResetCommand() : base((byte)'L')
    {
    }

    public override string ToString()
    {
        return nameof(LocalAdResetCommand);
    }

    protected override byte[] GetMessageBytes()
    {
        return new byte[] { 0x92 };
    }
}
