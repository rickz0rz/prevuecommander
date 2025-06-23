using YamlDotNet.Serialization;

namespace PrevueCommander.Model.PlaybookCommands.CommandObjects;

public record NewLookConfiguration
{
    [YamlMember] public char? RollAndHoldTime { get; init; }

    [YamlMember] public char GridSourceChannelOrder { get; init; } = 'C';

    [YamlMember] public char MRReviewInFSetting { get; init; } = '0';

    [YamlMember] public char SBSFrequency { get; init; } = '1';

    // 08
    // 08
    [YamlMember] public char DisplayMode { get; init; } = 'G';

    [YamlMember] public char AdvueInsertion { get; init; } = 'N';

    [YamlMember] public char PieFormat { get; init; } = 'A';

    [YamlMember] public char Language { get; init; } = 'E';

    [YamlMember] public char PrimeTimeSummary { get; init; } = '0';

    [YamlMember] public char SportsSummary { get; init; } = '1';

    [YamlMember] public char SumBySourceRollAndHold { get; init; } = 'N';

    [YamlMember] public char MovieRecapRollAndHold { get; init; } = 'N';

    [YamlMember] public char PrimeTimeSummaryRollAndHold { get; init; } = 'N';

    [YamlMember] public char SportsSummaryRollAndHold { get; init; } = 'N';

    [YamlMember] public char GridRollAndHold { get; init; } = 'N'; // Pause the grid every 3 listings

    [YamlMember] public char VideoVueInsertion { get; init; } = 'N';

    [YamlMember] public char LaserDiscInsertionType { get; init; } = 'L';

    [YamlMember] public int PrimeTimeStartingSlot { get; init; } = 29; // "29"

    [YamlMember] public int PrimeTimeLookAhead { get; init; } = 6; // "06"

    [YamlMember] public char CycleSummaryInfo { get; init; } = 'Y';

    [YamlMember] public char GridSynopsis { get; init; } = 'Y';

    [YamlMember] public char LockGridOnSynopsis { get; init; } = 'Y';

    [YamlMember] public int PrimeTimePromotionTimeSlotStart { get; init; } = 23;

    [YamlMember] public int PrimeTimePromotionTimeSlotEnd { get; init; } = 36;

    [YamlMember] public int LengthOfLookAheadSportsSummary { get; init; } = 6;

    [YamlMember] public int MinutesToDisplayPPVPostStart { get; init; } = 15;

    [YamlMember] public char SummaryCycleFrequency { get; init; } = '1';

    [YamlMember] public char WeatherHoldAndRoll { get; init; } = 'Y';

    [YamlMember] public char MilitaryTime { get; init; } = 'N';

    [YamlMember] public char CleanUpUnusedLogos { get; init; } = 'Y';

    [YamlMember] public char DisplayLength { get; init; } = 'C';

    [YamlMember] public char NumberOfColorPalettes { get; init; } = '8';

    [YamlMember] public char LocalTextAdSource { get; init; } = 'S';

    [YamlMember] public char PCDiskSupport { get; init; } = 'N';

    [YamlMember] public char ClockCommandInFSetting { get; init; } = '2';
}