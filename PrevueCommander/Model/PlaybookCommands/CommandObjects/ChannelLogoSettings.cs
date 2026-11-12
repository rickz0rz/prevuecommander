namespace PrevueCommander.Model.PlaybookCommands.CommandObjects;

/// <summary>
/// The channel logos of a guide data import. Each channel with a logo image gets a picture in
/// <c>DF0:Logos/Channels</c>, named by its source name. ESQ shows a channel logo with the call letters and the channel
/// number of the channel.
/// </summary>
public record ChannelLogoSettings
{
    /// <summary>
    /// The other lines of LOGO.LST, for example "Logos/tvgsport.uv,". The import sends a new LOGO.LST with these lines
    /// and a line for each channel logo.
    /// </summary>
    public List<string> KeepLines { get; init; } = [];

    /// <summary>
    /// True tells ESQ to read the new LOGO.LST at once. This needs <c>cleanUpUnusedLogos: 'N'</c> in the
    /// configuration. Without it, the logos show after a restart of ESQ.
    /// </summary>
    public bool Reload { get; init; }

    /// <summary>
    /// A directory that keeps the downloaded images, so that the next run does not download them again. A relative
    /// path starts at the directory of the playbook. Null downloads the images each time.
    /// </summary>
    public string? CacheDirectory { get; init; }

    /// <summary>A local directory that gets a copy of each IFF file.</summary>
    public string? SaveTo { get; init; }
}
