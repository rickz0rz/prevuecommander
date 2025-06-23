namespace Prevue.Commands;

public class SaveCommand : BaseCommand
{
    public SaveCommand() : base('%')
    {
    }

    public override string ToString()
    {
        return nameof(SaveCommand);
    }

    protected override byte[] GetMessageBytes()
    {
        return Array.Empty<byte>();
    }
}