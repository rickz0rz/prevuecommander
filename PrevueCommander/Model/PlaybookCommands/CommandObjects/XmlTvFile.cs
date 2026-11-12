namespace PrevueCommander.Model.PlaybookCommands.CommandObjects;

public record XmlTvFile
{
    /// <summary>The XMLTV file. A relative path starts at the directory of the playbook.</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>The maximum number of channels from this file. 0 means 200, the maximum of ESQ.</summary>
    public int MaximumNumberOfChannels { get; init; }
}
