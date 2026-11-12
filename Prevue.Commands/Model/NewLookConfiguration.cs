namespace Prevue.Commands.Model;

/// <summary>
/// The fields of the 'f' configuration record. The defaults are the values of the original data files. The comments
/// give the byte position in the record, and what ESQ does with the field if it is known
/// (<c>DISKIO_ParseConfigBuffer</c>).
/// </summary>
public record NewLookConfiguration
{
    /// <summary>0: the refresh interval in minutes.</summary>
    public char RollAndHoldTime { get; init; } = '2';

    /// <summary>1.</summary>
    public char GridSourceChannelOrder { get; init; } = 'C';

    /// <summary>2.</summary>
    public char MRReviewInFSetting { get; init; } = '0';

    /// <summary>3.</summary>
    public char SBSFrequency { get; init; } = '1';

    /// <summary>8: ESQ stores and saves this field, but its code does not use it.</summary>
    public char DisplayMode { get; init; } = 'G';

    /// <summary>9.</summary>
    public char AdvueInsertion { get; init; } = 'N';

    /// <summary>10.</summary>
    public char PieFormat { get; init; } = 'A';

    /// <summary>11.</summary>
    public char Language { get; init; } = 'E';

    /// <summary>12: a digit.</summary>
    public char PrimeTimeSummary { get; init; } = '0';

    /// <summary>13: a digit.</summary>
    public char SportsSummary { get; init; } = '1';

    /// <summary>14.</summary>
    public char SumBySourceRollAndHold { get; init; } = 'N';

    /// <summary>15.</summary>
    public char MovieRecapRollAndHold { get; init; } = 'N';

    /// <summary>16.</summary>
    public char PrimeTimeSummaryRollAndHold { get; init; } = 'N';

    /// <summary>17.</summary>
    public char SportsSummaryRollAndHold { get; init; } = 'N';

    /// <summary>18: 'Y' pauses the grid every 3 listings.</summary>
    public char GridRollAndHold { get; init; } = 'N';

    /// <summary>19.</summary>
    public char VideoVueInsertion { get; init; } = 'N';

    /// <summary>20: 'L', 'S' or 'V'.</summary>
    public char LaserDiscInsertionType { get; init; } = 'L';

    /// <summary>21-22: 0 to 99.</summary>
    public int PrimeTimeStartingSlot { get; init; } = 29;

    /// <summary>23-24: 0 to 99.</summary>
    public int PrimeTimeLookAhead { get; init; } = 6;

    /// <summary>25.</summary>
    public char CycleSummaryInfo { get; init; } = 'Y';

    /// <summary>26.</summary>
    public char GridSynopsis { get; init; } = 'Y';

    /// <summary>27.</summary>
    public char LockGridOnSynopsis { get; init; } = 'Y';

    /// <summary>28-29: 0 to 99.</summary>
    public int PrimeTimePromotionTimeSlotStart { get; init; } = 23;

    /// <summary>30-31: 0 to 99.</summary>
    public int PrimeTimePromotionTimeSlotEnd { get; init; } = 36;

    /// <summary>32-33: 0 to 99.</summary>
    public int LengthOfLookAheadSportsSummary { get; init; } = 6;

    /// <summary>34-36: 0 to 999.</summary>
    public int MinutesToDisplayPPVPostStart { get; init; } = 15;

    /// <summary>37: '1' to '9'.</summary>
    public char SummaryCycleFrequency { get; init; } = '1';

    /// <summary>40.</summary>
    public char WeatherHoldAndRoll { get; init; } = 'Y';

    /// <summary>41: 'Y' shows the clock in 24-hour format.</summary>
    public char MilitaryTime { get; init; } = 'N';

    /// <summary>42: 'Y' scans the logo directory.</summary>
    public char CleanUpUnusedLogos { get; init; } = 'Y';

    /// <summary>43: 'C' tells ESQ to use byte 44 for the banner.</summary>
    public char DisplayLength { get; init; } = 'C';

    /// <summary>45: ESQ always uses 8.</summary>
    public char NumberOfColorPalettes { get; init; } = '8';

    /// <summary>46: 'N', 'R', 'L' or 'S'. ESQ accepts local ads only with 'R' or 'S'.</summary>
    public char LocalTextAdSource { get; init; } = 'S';

    /// <summary>47: 'Y' mounts PC1: as the graphics drive.</summary>
    public char PCDiskSupport { get; init; } = 'N';

    /// <summary>51: '2' tells ESQ to accept the 'K' clock command. With '1', ESQ keeps its own clock.</summary>
    public char ClockCommandInFSetting { get; init; } = '2';
}
