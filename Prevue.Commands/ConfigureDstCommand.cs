namespace Prevue.Commands;

public class ConfigureDstCommand : BaseCommand
{
    // This seems to more closely match the data in dst.dat -- so maybe the command
    // should look more like Clock taking in a date and then calculating the DST start and stop
    // for said date dynamically. Should figure out what the extra bytes at the beginning
    // are used for though.
    //
    // https://prevueguide.com/Documentation/G2_Command.pdf
    // Weirdly there's two commands to handle this and G2 mentions specifically that it's based
    // on CST so you have to offset your values for other timezones. So ugly.
    //
    // See also:
    // https://prevueguide.com/Documentation/G3_Command.pdf
    //
    // For reference: daylight saving time starts on the second Sunday in March and ends on the first Sunday in November

    public ConfigureDstCommand() : base(new[] { 'g', '2' })
    {
    }

    public override string ToString()
    {
        return $"{nameof(ConfigureDstCommand)}";
    }

    protected override byte[] GetMessageBytes()
    {
        var payload = new List<byte>
        {
            0x04 // G2.inMarker (Control Code: ^D)
        };

        // DST Start: "202207103:00" (71st day, 3/12/2022 @ 3:00AM)
        payload.AddRange("202207103:00".ToCharArray().Select(c => (byte)c));
        // G2.outMarker (Control Code: ^S)
        payload.Add(0x13);
        // DST Start: "202230901:00" (309th day, 11/5/2022 @ 1:00AM)
        payload.AddRange("202230901:00".ToCharArray().Select(c => (byte)c));

        // Start the list off with the length of the payload + 1 in ASCII ("27")
        var messageBytes = new List<byte>($"{payload.Count + 1}".ToCharArray().Select(c => (byte)c));
        messageBytes.AddRange(payload);
        return messageBytes.ToArray();
    }
}