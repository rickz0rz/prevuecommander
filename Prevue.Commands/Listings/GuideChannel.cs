namespace Prevue.Commands.Listings;

/// <summary>A channel of a guide, before it becomes a channel of the lineup.</summary>
/// <param name="Number">The channel number, for example "4" or "56.2".</param>
/// <param name="CallSign">The call sign or the name of the channel.</param>
/// <param name="Programs">The programs of the channel.</param>
/// <param name="Premium">True to show the programs on a red background.</param>
/// <param name="LogoUrl">The address of a PNG image of the station, or null.</param>
public sealed record GuideChannel(string Number, string CallSign, IReadOnlyList<GuideProgram> Programs,
    bool Premium = false, string? LogoUrl = null);

/// <summary>A program of a guide.</summary>
/// <param name="Start">The local start time.</param>
/// <param name="End">The local end time.</param>
/// <param name="Text">The text for the grid, for example from <see cref="ProgramText"/>.</param>
/// <param name="IsMovie">True for a movie.</param>
/// <param name="IsStereo">True if the program has stereo sound.</param>
public sealed record GuideProgram(DateTime Start, DateTime End, string Text, bool IsMovie = false,
    bool IsStereo = false);
