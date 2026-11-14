using System.Net.Sockets;
using Prevue.Commands;
using PrevueCommander;
using PrevueCommander.Model;
using PrevueCommander.Model.PlaybookCommands;
using YamlDotNet.Core;

CommandLineOptions options;
try
{
    options = CommandLine.Parse(args);
}
catch (CommandLineException e)
{
    Console.Error.WriteLine(e.Message);
    return 5;
}

// The schema goes to the standard output alone, so that it can go to a file.
if (!options.Schema)
{
    Console.WriteLine("PrevueCommander - https://github.com/rickz0rz/prevuecommander");
    Console.WriteLine();
}

if (options.Help)
{
    Console.WriteLine(CommandLine.Usage);
    return 0;
}

if (options.Schema)
{
    if (options.SchemaTags)
    {
        foreach (var tag in PlaybookSchema.CustomTags())
            Console.WriteLine(tag);
    }
    else
    {
        Console.Write(PlaybookSchema.Generate());
    }

    return 0;
}

if (options.DecodePath != null)
{
    try
    {
        var feed = await File.ReadAllBytesAsync(options.DecodePath);
        var decoded = options.DecodeControl ? FeedDecoder.DecodeControl(feed) : FeedDecoder.DecodeData(feed);
        var errors = FeedDecoder.Write(decoded, Console.Out, options.Verbose);
        if (errors == 0)
            return 0;

        Console.Error.WriteLine($"{errors} commands have a bad checksum.");
        return 6;
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"Error: {e.Message}");
        return 4;
    }
}

var playbookPath = Path.GetFullPath(options.PlaybookPath);
if (!File.Exists(playbookPath))
{
    await File.WriteAllTextAsync(playbookPath, PlaybookLoader.Example());
    Console.WriteLine($"No playbook found. An example playbook is now at {playbookPath}.");
    Console.WriteLine("Edit it, and then run PrevueCommander again.");
    return 1;
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

try
{
    var playbook = PlaybookLoader.Parse(await File.ReadAllTextAsync(playbookPath, cancellation.Token));
    var configuration = options.Apply(playbook.Configuration);
    var now = DateTime.Now;
    if (!string.IsNullOrWhiteSpace(configuration.Now) && !DateTime.TryParse(configuration.Now,
            System.Globalization.CultureInfo.InvariantCulture, out now))
        throw new FormatException(
            $"The setting now is not a date and time: {configuration.Now}. Use a date such as 1993-02-23T18:00.");
    var context = new PlaybookContext(now, Path.GetDirectoryName(playbookPath)!, Console.Out)
    {
        Characters = configuration.CharacterSet
    };

    // Make all the commands first, so that an error in the playbook or in the guide data sends nothing.
    var commands = new List<BaseCommand>();
    foreach (var playbookCommand in playbook.Commands)
        commands.AddRange(await playbookCommand.TransformAsync(context, cancellation.Token));

    if (options.OutputPath != null)
    {
        var outputPath = Path.GetFullPath(options.OutputPath);
        var controlOutputPath = options.ControlOutputPath == null ? null : Path.GetFullPath(options.ControlOutputPath);
        var (data, control, leftOut) = await FeedFiles.WriteAsync(commands, outputPath, controlOutputPath,
            configuration.Output == Output.Verbose, Console.Out, cancellation.Token);
        Console.WriteLine($"Wrote {data} data line commands to {outputPath}.");
        if (controlOutputPath != null)
            Console.WriteLine($"Wrote {control} control line commands to {controlOutputPath}.");
        if (leftOut > 0)
            Console.WriteLine($"Left out {leftOut} control line commands. Use --control-output to write them.");
        return 0;
    }

    await using var lines = new SerialLines(configuration, Console.Out);
    lines.Check(commands);
    if (options.Validate)
    {
        // Render each command, so that a command with an error stops here too.
        foreach (var command in commands)
            Console.WriteLine(configuration.Output == Output.Verbose
                ? $"[{command}] {Convert.ToHexString(command.Render())}"
                : $"[{command}]");
        Console.WriteLine($"The playbook is valid. It has {commands.Count} commands.");
        return 0;
    }

    var progress = new TransferProgress(commands, configuration, Console.Out, TimeSpan.FromSeconds(30));
    progress.Start();
    foreach (var command in commands)
    {
        await lines.SendAsync(command, cancellation.Token);
        progress.Sent(command);
    }

    Console.WriteLine($"Sent {commands.Count} commands.");
    return 0;
}
catch (YamlException e)
{
    Console.Error.WriteLine($"The playbook {playbookPath} is not valid: {e.Message}");
    if (e.InnerException != null)
        Console.Error.WriteLine(e.InnerException.Message);
    return 2;
}
catch (SocketException e)
{
    Console.Error.WriteLine($"Cannot connect to the serial bridge: {e.Message}");
    return 3;
}
catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
{
    // HttpClient reports a timeout as a cancellation.
    Console.Error.WriteLine("Error: A download took too long.");
    return 4;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Stopped.");
    return 130;
}
catch (Exception e) when (e is ArgumentException or FormatException or InvalidOperationException or IOException
                              or InvalidDataException or HttpRequestException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Error: {e.Message}");
    return 4;
}
