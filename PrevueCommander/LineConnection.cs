using System.IO.Ports;
using System.Net.Sockets;

namespace PrevueCommander;

/// <summary>An open connection to one serial line of ESQ: a TCP bridge of an emulator, or a serial port.</summary>
public abstract class LineConnection : IAsyncDisposable
{
    public abstract Stream Stream { get; }

    public abstract ValueTask DisposeAsync();

    public static async Task<LineConnection> OpenTcpAsync(string hostname, int port,
        CancellationToken cancellationToken)
    {
        var client = new TcpClient();
        try
        {
            await client.ConnectAsync(hostname, port, cancellationToken);
            return new TcpLineConnection(client);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    /// <summary>Opens a serial port with 8 data bits, no parity and 1 stop bit, and no handshake.</summary>
    public static LineConnection OpenSerial(string portName, int baudRate)
    {
        if (baudRate <= 0)
            throw new ArgumentException($"The serial port {portName} needs a baud rate that is more than 0.");

        var port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One) { Handshake = Handshake.None };
        try
        {
            port.Open();
            return new SerialLineConnection(port);
        }
        catch
        {
            port.Dispose();
            throw;
        }
    }

    private sealed class TcpLineConnection(TcpClient client) : LineConnection
    {
        public override Stream Stream { get; } = client.GetStream();

        public override ValueTask DisposeAsync()
        {
            // Close the send direction first, so that the bridge gets all the bytes before the connection closes.
            try
            {
                client.Client.Shutdown(SocketShutdown.Send);
            }
            catch (SocketException)
            {
                // The bridge closed the connection already.
            }

            client.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SerialLineConnection(SerialPort port) : LineConnection
    {
        public override Stream Stream => port.BaseStream;

        public override async ValueTask DisposeAsync()
        {
            // Close the port only after the UART sent all the bytes. A close can discard the bytes in its buffer.
            try
            {
                while (port.IsOpen && port.BytesToWrite > 0)
                    await Task.Delay(10);
            }
            catch (InvalidOperationException)
            {
                // The port closed already.
            }

            port.Dispose();
        }
    }
}
