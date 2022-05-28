using Prevue.Commands.Model;

namespace Prevue.Commands;

public class NewLookConfigurationCommand : BaseCommand
{
    private readonly NewLookConfiguration _configuration;

    public NewLookConfigurationCommand() : base('f')
    {
        _configuration = new NewLookConfiguration();
    }

    public NewLookConfigurationCommand(NewLookConfiguration configuration) : base('f')
    {
        _configuration = configuration;
    }

    public override string ToString()
    {
        return nameof(NewLookConfigurationCommand);
    }

    protected override byte[] GetMessageBytes()
    {
        var bytes = new List<byte> { 0x00, 0x00 };

        bytes.AddRange($"62C010808{_configuration.DisplayMode}NAE01NNNNNNL2906YYY233606015100YNYC".ToCharArray().
            Select(c => (byte)c));
        bytes.Add(0x8E); // This particular character isn't ASCII
        bytes.AddRange("8SNNNN2".ToCharArray().Select(c => (byte)c));

        return bytes.ToArray();
    }
}
