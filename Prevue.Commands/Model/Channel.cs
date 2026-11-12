namespace Prevue.Commands.Model;

/// <summary>A channel of the lineup.</summary>
/// <param name="SourceName">
/// The name that the 'P' commands use to find the channel: 1 to 6 letters or digits, different for each channel.
/// </param>
/// <param name="ChannelNumber">The number that the grid shows, for example "4" or "56.2". It can be empty.</param>
/// <param name="CallSign">The name that the grid shows. ESQ keeps a maximum of 6 characters.</param>
/// <param name="Premium">True to show the programs of the channel on a red background.</param>
public sealed record Channel(string SourceName, string? ChannelNumber, string? CallSign, bool Premium = false)
{
    public const int MaximumSourceNameLength = 6;
    public const int MaximumChannelNumberLength = 9;
    public const int MaximumCallSignLength = 6;
}
