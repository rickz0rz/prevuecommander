namespace Prevue.Commands;

/// <summary>The serial inputs of ESQ.</summary>
public enum SerialLine
{
    /// <summary>The data line on the serial port: listings, ads, configuration and files.</summary>
    Data,

    /// <summary>The 110 baud control line (CTRL) on the CTS pin: the promos and logos of the top half.</summary>
    Control,

    /// <summary>No line. The command only changes the time of the commands that follow it.</summary>
    None
}
