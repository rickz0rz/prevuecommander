namespace Prevue.Commands;

public class AdditionalConfigurationCommand : BaseCommand
{
    public readonly byte _unknown;

    public AdditionalConfigurationCommand(byte unknown) : base((byte)'g')
    {
        _unknown = unknown;
    }

    public override string ToString()
    {
        return $"{nameof(AdditionalConfigurationCommand)}: Unknown Byte = 0x{_unknown:X2}";
    }

    protected override byte[] GetMessageBytes()
    {
        var messageBytes = new List<byte> { _unknown };

        messageBytes.AddRange(new Byte[]
        {
            0x32, 0x37, 0x04, 0x32, 0x30, 0x32, 0x32, 0x30, 0x37, 0x31, 0x30, 0x33, 0x3A, 0x30,
            0x30, 0x13, 0x32, 0x30, 0x32, 0x32, 0x33, 0x30, 0x39, 0x30, 0x31, 0x3A, 0x30, 0x30
        });

        return messageBytes.ToArray();
    }
}
