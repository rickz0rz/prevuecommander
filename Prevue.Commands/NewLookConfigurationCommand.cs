using System.Text;
using Prevue.Commands.Model;

namespace Prevue.Commands;

/// <summary>
/// The 'f' command: the configuration record. ESQ applies it and saves it to config.dat.
/// </summary>
/// <remarks>
/// The payload is a byte that ESQ does not use, the length of the record as a 16-bit big-endian number, and the 52
/// bytes of the record. ESQ reads (length - 1) bytes after the length: the record and the NUL
/// terminator. So the length is the length of the record plus 2.
/// </remarks>
public class NewLookConfigurationCommand(NewLookConfiguration? configuration = null) : BaseCommand('f')
{
    private const int RecordLength = 52;

    private readonly NewLookConfiguration _configuration = configuration ?? new NewLookConfiguration();

    public override string ToString() =>
        $"{nameof(NewLookConfigurationCommand)}: Display mode = {_configuration.DisplayMode}, " +
        $"Local ads = {_configuration.LocalTextAdSource}, Clock command = {_configuration.ClockCommandInFSetting}";

    protected override byte[] GetPayload()
    {
        var record = Record(_configuration);
        var length = record.Length + 2;
        return [0x00, (byte)(length >> 8), (byte)length, .. record];
    }

    /// <summary>The 52 bytes of the record.</summary>
    public static byte[] Record(NewLookConfiguration c)
    {
        var text = new StringBuilder()
            .Append(c.RollAndHoldTime).Append(c.GridSourceChannelOrder).Append(c.MRReviewInFSetting)
            .Append(c.SBSFrequency)
            .Append("0808") // 4-5: a slot number, 6-7: the span of the grid in half hours
            .Append(c.DisplayMode).Append(c.AdvueInsertion).Append(c.PieFormat).Append(c.Language)
            .Append(c.PrimeTimeSummary).Append(c.SportsSummary)
            .Append(c.SumBySourceRollAndHold).Append(c.MovieRecapRollAndHold).Append(c.PrimeTimeSummaryRollAndHold)
            .Append(c.SportsSummaryRollAndHold).Append(c.GridRollAndHold).Append(c.VideoVueInsertion)
            .Append(c.LaserDiscInsertionType)
            .Append(Number(c.PrimeTimeStartingSlot, 2)).Append(Number(c.PrimeTimeLookAhead, 2))
            .Append(c.CycleSummaryInfo).Append(c.GridSynopsis).Append(c.LockGridOnSynopsis)
            .Append(Number(c.PrimeTimePromotionTimeSlotStart, 2)).Append(Number(c.PrimeTimePromotionTimeSlotEnd, 2))
            .Append(Number(c.LengthOfLookAheadSportsSummary, 2)).Append(Number(c.MinutesToDisplayPPVPostStart, 3))
            .Append(c.SummaryCycleFrequency)
            .Append("00") // 38-39: a brush label
            .Append(c.WeatherHoldAndRoll).Append(c.MilitaryTime).Append(c.CleanUpUnusedLogos).Append(c.DisplayLength)
            .ToString();

        byte[] record =
        [
            .. Encoding.ASCII.GetBytes(text),
            0x8E, // 44: the banner value that 'C' at byte 43 selects
            (byte)c.NumberOfColorPalettes, (byte)c.LocalTextAdSource, (byte)c.PCDiskSupport,
            .. "NNN"u8, // 48-50
            (byte)c.ClockCommandInFSetting
        ];

        if (record.Length != RecordLength || record.Any(b => b == 0))
            throw new ArgumentException("A field of the 'f' configuration is empty or not valid.", nameof(c));
        return record;
    }

    private static string Number(int value, int digits)
    {
        var maximum = (int)Math.Pow(10, digits) - 1;
        if (value < 0 || value > maximum)
            throw new ArgumentOutOfRangeException(nameof(value), value, $"The value must be 0 to {maximum}.");
        return value.ToString($"D{digits}");
    }
}
