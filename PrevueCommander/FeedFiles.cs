using Prevue.Commands;

namespace PrevueCommander;

/// <summary>
/// Writes the bytes of the commands to files in place of the serial lines, for a dry run. A file has the bytes as ESQ
/// reads them, with no pauses, so that an emulator can replay it, for example with <c>Esquire --serial-file</c>.
/// </summary>
public static class FeedFiles
{
    /// <summary>Writes the data line commands to <paramref name="dataPath"/>, and the control line commands to
    /// <paramref name="controlPath"/>. Without <paramref name="controlPath"/>, the control line commands are left out.
    /// </summary>
    /// <returns>The number of commands in each file, and the number of control line commands that were left out.</returns>
    public static async Task<(int Data, int Control, int LeftOut)> WriteAsync(IEnumerable<BaseCommand> commands,
        string dataPath, string? controlPath, bool verbose, TextWriter log, CancellationToken cancellationToken)
    {
        await using var data = File.Create(dataPath);
        await using var control = controlPath == null ? null : File.Create(controlPath);
        int dataCount = 0, controlCount = 0, leftOut = 0;
        foreach (var command in commands)
        {
            var stream = command.Line switch
            {
                SerialLine.Data => data,
                SerialLine.Control => control,
                _ => null
            };

            if (stream == null)
            {
                if (command.Line == SerialLine.Control)
                    leftOut++;
                continue;
            }

            var bytes = command.Render();
            log.WriteLine(verbose ? $"[{command}] {Convert.ToHexString(bytes)}" : $"[{command}]");
            await stream.WriteAsync(bytes, cancellationToken);
            if (command.Line == SerialLine.Data)
                dataCount++;
            else
                controlCount++;
        }

        return (dataCount, controlCount, leftOut);
    }
}
