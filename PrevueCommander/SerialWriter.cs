using System.Diagnostics;
using Prevue.Commands;

namespace PrevueCommander;

/// <summary>
/// Sends commands to the serial line of ESQ at the speed of the line. ESQ has no flow control: it loses data when its
/// receive buffer is full.
/// </summary>
/// <param name="stream">The connection to the serial bridge.</param>
/// <param name="baudRate">The speed of the line. A byte takes 10 bits (8N1). 0 sends at full speed.</param>
/// <param name="commandDelay">The pause after each command.</param>
/// <param name="verbose">True to also show the bytes of each command.</param>
/// <param name="log">The output for messages.</param>
public sealed class SerialWriter(Stream stream, int baudRate, TimeSpan commandDelay, bool verbose, TextWriter log)
{
    private const int BitsPerByte = 10;
    private const int ChunkSize = 16;

    public async Task SendAsync(BaseCommand command, CancellationToken cancellationToken)
    {
        var bytes = command.Render();
        log.WriteLine(verbose ? $"[{command}] {Convert.ToHexString(bytes)}" : $"[{command}]");

        var clock = Stopwatch.StartNew();
        for (var offset = 0; offset < bytes.Length; offset += ChunkSize)
        {
            var count = Math.Min(ChunkSize, bytes.Length - offset);
            await stream.WriteAsync(bytes.AsMemory(offset, count), cancellationToken);
            if (baudRate <= 0)
                continue;

            var due = TimeSpan.FromSeconds((double)(offset + count) * BitsPerByte / baudRate);
            if (due > clock.Elapsed)
                await Task.Delay(due - clock.Elapsed, cancellationToken);
        }

        await stream.FlushAsync(cancellationToken);
        if (commandDelay > TimeSpan.Zero)
            await Task.Delay(commandDelay, cancellationToken);
    }
}
