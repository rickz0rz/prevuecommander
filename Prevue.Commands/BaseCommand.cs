namespace Prevue.Commands;

public class BaseCommand
{
    private readonly byte[] _commandCodes;

    protected BaseCommand(byte commandCode)
    {
        _commandCodes = new[] { commandCode };
    }

    protected BaseCommand(byte[] commandCodes)
    {
        _commandCodes = commandCodes;
    }

    protected BaseCommand(char commandCode)
    {
        _commandCodes = new[] { (byte)commandCode };
    }

    protected BaseCommand(char[] commandCodes)
    {
        _commandCodes = commandCodes.Select(c => (byte)c).ToArray();
    }

    protected virtual byte[] GetMessageBytes()
    {
        throw new NotImplementedException("Can't render the message bytes of the BaseCommand.");
    }

    public override string ToString()
    {
        return nameof(BaseCommand);
    }

    public byte[] Render()
    {
        var bytes = new List<byte>();

        bytes.AddRange(new byte[] { 0x55, 0xAA });
        bytes.AddRange(_commandCodes);
        bytes.AddRange(GetMessageBytes());
        bytes.Add(0x00); // Terminator

        var checkSum = bytes.Aggregate(0, (current, t) => current ^ t);
        bytes.Add((byte)checkSum);

        return bytes.ToArray();
    }
}
