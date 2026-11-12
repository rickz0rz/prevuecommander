namespace PrevueCommander.Model;

public enum Output
{
    /// <summary>Show each command.</summary>
    Minimal,

    /// <summary>Show each command and its bytes.</summary>
    Verbose
}

/// <summary>The connection to the serial line of ESQ.</summary>
public record Configuration
{
    /// <summary>The host name or IP address of the serial bridge of the emulator.</summary>
    public string Hostname { get; init; } = "127.0.0.1";

    /// <summary>The TCP port of the serial bridge.</summary>
    public int Port { get; init; } = 5400;

    public Output Output { get; init; } = Output.Minimal;

    /// <summary>
    /// The speed of the line. PrevueCommander sends a maximum of baudRate / 10 bytes each second. 0 sends at full
    /// speed: use it only when the emulator paces the line, because ESQ loses data when its buffer is full.
    /// </summary>
    public int BaudRate { get; init; } = 2400;

    /// <summary>The time in milliseconds between two commands.</summary>
    public int CommandDelay { get; init; } = 100;
}
