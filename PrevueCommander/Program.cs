using System.Net.Sockets;
using Prevue.Commands;
using PrevueCommander;
using PrevueCommander.Model;
using PrevueCommander.Model.PlaybookCommands;
using YamlDotNet.Core;

Console.WriteLine("PrevueCommander - https://github.com/rickz0rz/prevuecommander");
Console.WriteLine();

var playbookPath = Path.GetFullPath(args.Length == 0 ? "playbook.yaml" : args[0]);
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
    var now = DateTime.Now;
    if (!string.IsNullOrWhiteSpace(playbook.Configuration.Now) && !DateTime.TryParse(playbook.Configuration.Now,
            System.Globalization.CultureInfo.InvariantCulture, out now))
        throw new FormatException(
            $"The setting now is not a date and time: {playbook.Configuration.Now}. Use a date such as 1993-02-23T18:00.");
    var context = new PlaybookContext(now, Path.GetDirectoryName(playbookPath)!, Console.Out)
    {
        Characters = playbook.Configuration.CharacterSet
    };

    // Make all the commands first, so that an error in the playbook or in the guide data sends nothing.
    var commands = new List<BaseCommand>();
    foreach (var playbookCommand in playbook.Commands)
        commands.AddRange(await playbookCommand.TransformAsync(context, cancellation.Token));

    await using var lines = new SerialLines(playbook.Configuration, Console.Out);
    lines.Check(commands);
    foreach (var command in commands)
        await lines.SendAsync(command, cancellation.Token);

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
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Stopped.");
    return 130;
}
catch (Exception e) when (e is ArgumentException or FormatException or InvalidOperationException or IOException
                              or HttpRequestException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Error: {e.Message}");
    return 4;
}
