namespace Prevue.Commands;

public class ConfigureDstCommand : BaseCommand
{
    // This seems to more closely match the data in dst.dat -- so maybe the command
    // should look more like Clock taking in a date and then calculating the DST start and stop
    // for said date dynamically. Should figure out what the extra bytes at the beginning
    // are used for though.
    //
    // https://prevueguide.com/Documentation/G2_Command.pdf
    // https://prevueguide.com/Documentation/G3_Command.pdf

    public readonly byte _unknown;

    // daylight saving time starts on the second Sunday in March and ends on the first Sunday in November

    public ConfigureDstCommand(byte unknown) : base((byte)'g')
    {
        _unknown = unknown;
    }

    public override string ToString()
    {
        return $"{nameof(ConfigureDstCommand)}: Unknown Byte = 0x{_unknown:X2}";
    }

    protected override byte[] GetMessageBytes()
    {
        var messageBytes = new List<byte> { _unknown };

        messageBytes.AddRange(new Byte[]
        {
            // 0x32, 0x33 so "227" then "228" ?
            0x32, 0x37, // "27"
            0x04, // ?
            0x32, 0x30, 0x32, 0x32, 0x30, 0x37, 0x31, 0x30, 0x33, 0x3A, 0x30, 0x30, // "202207103:00" (2022, 71st day, 3:00AM)
            0x13, // ' '
            0x32, 0x30, 0x32, 0x32, 0x33, 0x30, 0x39, 0x30, 0x31, 0x3A, 0x30, 0x30 // "202230901:00" (2022, 309th day, 1:00AM)
        });

        return messageBytes.ToArray();
    }
}
