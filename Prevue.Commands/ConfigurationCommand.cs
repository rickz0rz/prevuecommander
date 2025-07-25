namespace Prevue.Commands;

public class ConfigurationCommand : BaseCommand
{
    public ConfigurationCommand() : base('F')
    {
    }

    public override string ToString()
    {
        return nameof(ConfigurationCommand);
    }

    protected override byte[] GetMessageBytes()
    {
        return new byte[]
        {
            0x42, 0x45, 0x33, 0x33, 0x36, 0x36, 0x4E, 0x01, 0x01, 0x37, 0x59, 0x59, 0x4E, 0x4E,
            0x4E, 0x59, 0x41, 0x4E, 0x4E, 0x00
        };
    }
}