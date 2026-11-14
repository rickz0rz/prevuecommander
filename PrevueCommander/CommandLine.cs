using System.Globalization;
using PrevueCommander.Model;

namespace PrevueCommander;

/// <summary>An error in the command-line arguments.</summary>
public sealed class CommandLineException(string message) : Exception(message);

/// <summary>The options of the command line. A setting that is set here replaces the setting of the playbook.</summary>
public sealed record CommandLineOptions
{
    public string PlaybookPath { get; init; } = "playbook.yaml";

    /// <summary>True to show the help and stop.</summary>
    public bool Help { get; init; }

    /// <summary>True to make the commands and stop without a connection.</summary>
    public bool Validate { get; init; }

    public string? Hostname { get; init; }

    public int? Port { get; init; }

    public string? SerialPort { get; init; }

    public int? BaudRate { get; init; }

    public int? ControlPort { get; init; }

    public string? ControlSerialPort { get; init; }

    public int? ControlBaudRate { get; init; }

    public string? Now { get; init; }

    public bool Verbose { get; init; }

    /// <summary>A file for the bytes of the data line, in place of the connection. Null sends the commands.</summary>
    public string? OutputPath { get; init; }

    /// <summary>A file for the bytes of the control line, with <see cref="OutputPath"/>.</summary>
    public string? ControlOutputPath { get; init; }

    /// <summary>The configuration of the playbook, with the settings of the command line in place of its settings.</summary>
    public Configuration Apply(Configuration configuration) => configuration with
    {
        Hostname = Hostname ?? configuration.Hostname,
        Port = Port ?? configuration.Port,
        // A port of one kind replaces the other kind, so that the command line can select the TCP bridge.
        SerialPort = SerialPort ?? (Port != null ? null : configuration.SerialPort),
        BaudRate = BaudRate ?? configuration.BaudRate,
        ControlPort = ControlPort ?? configuration.ControlPort,
        ControlSerialPort = ControlSerialPort ?? (ControlPort != null ? null : configuration.ControlSerialPort),
        ControlBaudRate = ControlBaudRate ?? configuration.ControlBaudRate,
        Now = Now ?? configuration.Now,
        Output = Verbose ? Output.Verbose : configuration.Output
    };
}

/// <summary>Reads the command-line arguments.</summary>
public static class CommandLine
{
    public const string Usage = """
        Usage: PrevueCommander [options] [playbook]

        Sends the commands of a playbook to ESQ. The default playbook is playbook.yaml in the current directory.
        If the playbook does not exist, PrevueCommander writes an example playbook to that path.

        Options:
          --host <name>                 The host of the TCP bridges (hostname).
          --port <n>                    The TCP port of the data line (port). It replaces serialPort.
          --serial-port <name>          A serial port for the data line (serialPort), for example COM3.
          --baud-rate <n>               The speed of the data line (baudRate). 0 sends at full speed.
          --control-port <n>            The TCP port of the control line (controlPort). It replaces controlSerialPort.
          --control-serial-port <name>  A serial port for the control line (controlSerialPort).
          --control-baud-rate <n>       The speed of the control line (controlBaudRate).
          --now <date>                  The time of the run (now), for example 1993-02-23T18:00.
          --verbose                     Show the bytes of each command (output: Verbose).
          --validate                    Make all the commands, and stop. Do not connect.
          --output <file>               Write the bytes of the data line to the file. Do not connect.
          --control-output <file>       With --output, write the bytes of the control line to the file.
          -h, --help                    Show this help.

        An option replaces the setting of the playbook in parentheses.
        """;

    public static CommandLineOptions Parse(IReadOnlyList<string> args)
    {
        var options = new CommandLineOptions();
        string? playbook = null;
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith('-') || arg == "-")
            {
                if (playbook != null)
                    throw new CommandLineException($"Give only one playbook. The arguments have {playbook} and {arg}.");
                playbook = arg;
                continue;
            }

            // Both "--port 5401" and "--port=5401" are correct.
            string? inlineValue = null;
            var equals = arg.IndexOf('=');
            if (equals > 0)
            {
                inlineValue = arg[(equals + 1)..];
                arg = arg[..equals];
            }

            string Value()
            {
                if (inlineValue != null)
                    return inlineValue;
                if (i + 1 >= args.Count)
                    throw new CommandLineException($"The option {arg} needs a value.");
                return args[++i];
            }

            int Number()
            {
                var value = Value();
                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                    throw new CommandLineException($"The option {arg} needs a number of 0 or more, not {value}.");
                return number;
            }

            void NoValue()
            {
                if (inlineValue != null)
                    throw new CommandLineException($"The option {arg} has no value.");
            }

            switch (arg)
            {
                case "-h" or "--help":
                    NoValue();
                    options = options with { Help = true };
                    break;
                case "--validate":
                    NoValue();
                    options = options with { Validate = true };
                    break;
                case "--verbose":
                    NoValue();
                    options = options with { Verbose = true };
                    break;
                case "--host":
                    options = options with { Hostname = Value() };
                    break;
                case "--port":
                    options = options with { Port = Number() };
                    break;
                case "--serial-port":
                    options = options with { SerialPort = Value() };
                    break;
                case "--baud-rate":
                    options = options with { BaudRate = Number() };
                    break;
                case "--control-port":
                    options = options with { ControlPort = Number() };
                    break;
                case "--control-serial-port":
                    options = options with { ControlSerialPort = Value() };
                    break;
                case "--control-baud-rate":
                    options = options with { ControlBaudRate = Number() };
                    break;
                case "--output":
                    options = options with { OutputPath = Value() };
                    break;
                case "--control-output":
                    options = options with { ControlOutputPath = Value() };
                    break;
                case "--now":
                    options = options with { Now = Value() };
                    break;
                default:
                    throw new CommandLineException($"The option {arg} is not known. Use --help to show the options.");
            }
        }

        if (options.ControlOutputPath != null && options.OutputPath == null)
            throw new CommandLineException("The option --control-output needs --output.");
        if (options.Validate && options.OutputPath != null)
            throw new CommandLineException("Use --validate or --output, not both.");

        return playbook == null ? options : options with { PlaybookPath = playbook };
    }
}
