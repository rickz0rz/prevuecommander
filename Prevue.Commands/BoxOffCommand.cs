namespace Prevue.Commands;

public class BoxOffCommand : BaseCommand
{
    public BoxOffCommand() : base(0xBB)
    {
    }

    protected override byte[] GetMessageBytes()
    {
        return new byte[] { 0xBB };
    }
}