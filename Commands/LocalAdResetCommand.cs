namespace PrevueCommander.Commands;

public class LocalAdResetCommand : BaseCommand
{
    public LocalAdResetCommand() : base((byte)'L')
    {
    }

    protected override byte[] GetMessageBytes()
    {
        return new byte[] { 0x92 };
    }
}