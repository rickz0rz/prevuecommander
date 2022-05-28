namespace Prevue.Commands.Model;

public record NewLookConfiguration
{
    public char RollAndHoldTime { get; init; } = '2';
    public char GridSourceChannelOrder { get; init; } = 'C';
    public char MRReviewInFSetting { get; init; } = '0';
    public char SBSFrequency { get; init; } = '1';
    // 08
    // 08
    public char DisplayMode { get; init; } = 'G';
    public char AdvueInsertion { get; init; } = 'N';
    public char PieFormat { get; init; } = 'A';
    public char Language { get; init; } = 'E';
    public char PrimeTimeSummary { get; init; } = '0';
    public char SportsSummary { get; init; } = '1';
    public char SumBySourceRollAndHold { get; init; } = 'N';
    public char MovieRecapRollAndHold { get; init; } = 'N';
    public char PrimeTimeSummaryRollAndHold { get; init; } = 'N';
    public char SportsSummaryRollAndHold { get; init; } = 'N';
    public char GridRollAndHold { get; init; } = 'N'; // Pause the grid every 3 listings
    public char VideoVueInsertion { get; init; } = 'N';
    public char LaserDiscInsertionType { get; init; } = 'L';
    public int PrimeTimeStartingSlot { get; init; } = 29; // "29"
    public int PrimeTimeLookAhead { get; init; } = 6; // "06"
    public char CycleSummaryInfo { get; init; } = 'Y';
    public char GridSynopsis { get; init; } = 'Y';
    public char LockGridOnSynopsis { get; init; } = 'Y';
    public int PrimeTimePromotionTimeSlotStart { get; init; } = 23; // "23"
    public int PrimeTimePromotionTimeSlotEnd { get; init; } = 36; // "36"
    public int LengthOfLookAheadSportsSummary { get; init; } = 6; // "06"
    public int MinutesToDisplayPPVPostStart { get; init; } = 15; // "015"
    public char SummaryCycleFrequency { get; init; } = '1';
    // 00
    public char WeatherHoldAndRoll { get; init; } = 'Y';
    public char MilitaryTime { get; init; } = 'N';
    public char CleanUpUnusedLogos { get; init; } = 'Y';
    public char DisplayLength { get; init; } = 'C';
    // 0x8E
    public char NumberOfColorPalettes { get; init; } = '8';
    public char LocalTextAdSource { get; init; } = 'S';
    public char PCDiskSupport { get; init; } = 'N';
    // NNN
    public char ClockCommandInFSetting { get; init; } = '2';
}
