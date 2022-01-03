namespace Prevue.Commands;

public class BaseCommand
{
    private readonly byte _commandCode;

    protected BaseCommand(byte commandCode)
    {
        _commandCode = commandCode;
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
        bytes.Add(_commandCode);
        bytes.AddRange(GetMessageBytes());
        bytes.Add(0x00); // Terminator

        var checkSum = bytes.Aggregate(0, (current, t) => current ^ t);
        bytes.Add((byte)checkSum);

        return bytes.ToArray();
    }
}
