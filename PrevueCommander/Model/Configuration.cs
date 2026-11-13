using Prevue.Core;

namespace PrevueCommander.Model;

public enum Output
{
    /// <summary>Show each command.</summary>
    Minimal,

    /// <summary>Show each command and its bytes.</summary>
    Verbose
}

/// <summary>The connections to the serial lines of ESQ.</summary>
public record Configuration
{
    /// <summary>The host name or IP address of the serial bridge of the emulator.</summary>
    public string Hostname { get; init; } = "127.0.0.1";

    /// <summary>The TCP port of the serial bridge.</summary>
    public int Port { get; init; } = 5400;

    /// <summary>
    /// A serial port of this computer for the data line, for example "/dev/tty.usbserial-1410" or "COM3". When it is
    /// set, Prevue Commander writes to this port at <see cref="BaudRate"/>, and it does not use the TCP bridge.
    /// </summary>
    public string? SerialPort { get; init; }

    public Output Output { get; init; } = Output.Minimal;

    /// <summary>
    /// The speed of the line. PrevueCommander sends a maximum of baudRate / 10 bytes each second. 0 sends at full
    /// speed: use it only when the emulator paces the line, because ESQ loses data when its buffer is full.
    /// </summary>
    public int BaudRate { get; init; } = 2400;

    /// <summary>The time in milliseconds between two commands.</summary>
    public int CommandDelay { get; init; } = 100;

    /// <summary>
    /// The TCP port of the control line bridge, for the promo and logo commands. Esquire opens it with
    /// <c>--prevue-ctrl-port</c>. 0 means no control line.
    /// </summary>
    public int ControlPort { get; init; }

    /// <summary>
    /// A serial port of this computer for the control line. When it is set, Prevue Commander writes to this port at
    /// <see cref="ControlBaudRate"/>, and it does not use <see cref="ControlPort"/>.
    /// </summary>
    public string? ControlSerialPort { get; init; }


    /// <summary>
    /// The speed of the control line. ESQ reads it at 110 baud. 0 sends at full speed, for a bridge that keeps the
    /// bytes until the line sends them, as Esquire does.
    /// </summary>
    public int ControlBaudRate { get; init; } = 110;

    /// <summary>
    /// A local date and time that the playbook uses in place of the clock of this computer, for example
    /// "1993-02-23T18:00". The imports send the listings of the broadcast day of this time and of the next day.
    /// <c>!!clock</c> without a date sends this time, and <c>!!configureDst</c> without a year uses its year. Null uses
    /// the clock of this computer.
    /// </summary>
    public string? Now { get; init; }

    /// <summary>
    /// The characters that the text of the listings, the local ads and the promos can have. Latin1 keeps letters with
    /// accents, for example "é", as the fonts of ESQ 9.0.14 have them. Ascii removes the accents, for older versions.
    /// </summary>
    public CharacterSet CharacterSet { get; init; } = CharacterSet.Latin1;
}
