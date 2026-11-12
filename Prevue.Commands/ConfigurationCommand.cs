using Prevue.Commands.Model;

namespace Prevue.Commands;

/// <summary>
/// The 'F' command: the status packet. ESQ accepts 'C' commands only after it receives an 'F' command.
/// </summary>
/// <remarks>
/// ESQ reads exactly 20 bytes and the NUL (<c>_ESQIFF2_ApplyIncomingStatusPacket</c>). The names come from the
/// diagnostics screen of ESQ. The values that a playbook cannot change are those of the original data files.
/// </remarks>
public class ConfigurationCommand : BaseCommand
{
    private readonly StatusConfiguration _configuration;

    public ConfigurationCommand(StatusConfiguration? configuration = null) : base('F')
    {
        _configuration = configuration ?? new StatusConfiguration();
        if (_configuration.TimeZone is not (>= '1' and <= '9' or >= 'A' and <= 'H'))
            throw new ArgumentException("The time zone must be '1' to 'H'.", nameof(configuration));
        if (_configuration.ClockOffsetMinutes is < 0 or > 59)
            throw new ArgumentException("The clock offset must be 0 to 59 minutes.", nameof(configuration));
    }

    public override string ToString() =>
        $"{nameof(ConfigurationCommand)}: Time zone = {_configuration.TimeZone}, DST = {_configuration.DaylightSavingTime}";

    protected override byte[] GetPayload() =>
    [
        (byte)'B', // BCK: back slots
        (byte)'E', // FWD: forward slots
        (byte)_configuration.ScrollSpeed, // SSPD
        (byte)'3', (byte)'6', // #AD
        (byte)'6', // LINE
        (byte)'N',
        0x01, 0x01, // Minute event seeds
        (byte)_configuration.TimeZone, // TZ
        (byte)(_configuration.DaylightSavingTime ? 'Y' : 'N'), // DST
        (byte)'Y', // CONT
        (byte)'N',
        (byte)'N', (byte)'N', // Alert modes
        (byte)'Y', (byte)'A',
        (byte)'N', // GRPH
        (byte)'N', // VIN
        (byte)_configuration.ClockOffsetMinutes
    ];
}
