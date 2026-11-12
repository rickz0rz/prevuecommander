using Prevue.Commands;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// Sends a local file to the drive of ESQ. ESQ accepts one file transfer for each address command, and it ignores
/// an address command while a unit is selected. So this command sends a box off command and an address command
/// before the file.
/// </summary>
/// <remarks>
/// For a target in a directory, the command first makes the directories in RAM: and on the target volume with the
/// AmigaDOS command MakeDir, in a session of its own. ESQ writes the temporary file in RAM:, and then copies it to the
/// target. MakeDir fails without harm if a directory exists.
/// </remarks>
public record FileTransferPlaybookCommand : IPlaybookCommand
{
    /// <summary>The local file. A relative path starts at the directory of the playbook.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>The path on the Amiga, for example "DF0:Logos/MYAD.IFF".</summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>The address that selects the units again before the transfer.</summary>
    public string Address { get; init; } = "*";

    public async Task<IReadOnlyList<BaseCommand>> TransformAsync(PlaybookContext context,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllBytesAsync(context.ResolvePath(Source), cancellationToken);
        return Commands(Target, content, Address);
    }

    /// <summary>The commands that send <paramref name="content"/> to <paramref name="target"/> on the drive.</summary>
    public static IReadOnlyList<BaseCommand> Commands(string target, byte[] content, string address = "*")
    {
        var transfer = new FileTransferCommand(target, content);

        var commands = new List<BaseCommand>();
        var directories = transfer.Directories().ToList();
        if (directories.Count > 0)
        {
            commands.Add(new BoxOffCommand());
            commands.Add(new AddressCommand(address));
            commands.AddRange(directories.Select(d => UtilityCommand.Shell($"MakeDir {d}")));
        }

        commands.AddRange([new BoxOffCommand(), new AddressCommand(address), transfer]);
        return commands;
    }
}
