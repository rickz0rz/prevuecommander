namespace PrevueCommander.Model.PlaybookCommands.CommandObjects;

public record ChannelsDVRServer
{
    /// <summary>The address of the server, for example "http://192.168.0.10:8089".</summary>
    public string ServerAddress { get; init; } = string.Empty;

    /// <summary>The maximum number of channels from this server. 0 means 200, the maximum of ESQ.</summary>
    public int MaximumNumberOfChannels { get; init; }

    /// <summary>True to import only the HD channels.</summary>
    public bool FilterOnlyHDChannels { get; init; }

    /// <summary>True to leave out the Music Choice channels (call signs that start with "MC").</summary>
    public bool FilterMusicChoiceChannels { get; init; } = true;
}
