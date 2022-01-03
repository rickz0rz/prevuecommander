using System.Collections.Concurrent;
using System.Net.Sockets;
using Prevue.Commands;

namespace PrevueCommander;

public class DataWriter
{
    private readonly ConcurrentQueue<BaseCommand> _commandQueue;
    private readonly Socket _socket;
    private readonly int _simulatedBaudRate;
    private readonly TimeSpan _delayBetweenBytes;
    private readonly Task _task;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public DataWriter(Socket socket, int simulatedBaudRate = 2400)
    {
        _commandQueue = new ConcurrentQueue<BaseCommand>();
        _socket = socket;
        _simulatedBaudRate = simulatedBaudRate;
        _delayBetweenBytes = new TimeSpan(new TimeSpan(0, 0, 1).Ticks / _simulatedBaudRate);
        _cancellationTokenSource = new CancellationTokenSource();

        _task = Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                while (_commandQueue.TryDequeue(out var command))
                {
                    Console.Write($"[{command}]");

                    foreach (var currentCommandByte in command.Render())
                    {
                        Console.Write($" {currentCommandByte:X2}");
                        await _socket.SendAsync(new[] { currentCommandByte }, SocketFlags.None);
                        await Task.Delay(_delayBetweenBytes);
                    }

                    Console.WriteLine();

                    await Task.Delay(500);
                }
            }
        });
    }

    public void AddCommandToBuffer(BaseCommand baseCommand)
    {
        _commandQueue.Enqueue(baseCommand);
    }

    public void FlushBuffer()
    {
        _cancellationTokenSource.Cancel();
        Task.WaitAll(new[] { _task });
    }
}
