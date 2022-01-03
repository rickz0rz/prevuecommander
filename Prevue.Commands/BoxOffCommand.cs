namespace Prevue.Commands;

public class BoxOffCommand : BaseCommand
{
    public BoxOffCommand() : base(0xBB)
    {
    }

    public override string ToString()
    {
        return nameof(BoxOffCommand);
    }

    protected override byte[] GetMessageBytes()
    {
        return new byte[] { 0xBB };
    }
}
