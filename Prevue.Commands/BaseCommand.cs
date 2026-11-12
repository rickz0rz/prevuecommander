using System.Text;

namespace Prevue.Commands;

/// <summary>
/// A command of the Prevue serial data feed. ESQ reads a command as <c>55 AA</c>, the command code, the payload, a NUL
/// byte and a checksum. The checksum is the XOR of all the bytes before it, which is <c>$FF</c> XOR the command code
/// XOR the payload (<c>_ESQ_GenerateXorChecksumByte</c>).
/// </summary>
public abstract class BaseCommand
{
    private static readonly byte[] Preamble = [0x55, 0xAA];

    private readonly byte[] _commandCode;

    protected BaseCommand(params byte[] commandCode)
    {
        _commandCode = commandCode;
    }

    protected BaseCommand(char commandCode) : this((byte)commandCode)
    {
    }

    /// <summary>The bytes between the command code and the NUL terminator.</summary>
    protected abstract byte[] GetPayload();

    /// <summary>The serial line of ESQ that reads the command.</summary>
    public virtual SerialLine Line => SerialLine.Data;

    /// <summary>The bytes of the command, ready to send.</summary>
    public virtual byte[] Render() => Frame([.. _commandCode, .. GetPayload(), 0x00]);

    public override string ToString() => GetType().Name;

    /// <summary>Puts the preamble before the bytes and the checksum after them.</summary>
    protected static byte[] Frame(byte[] body)
    {
        byte[] frame = [.. Preamble, .. body, 0x00];
        frame[^1] = Checksum(frame.AsSpan(0, frame.Length - 1));
        return frame;
    }

    protected static byte Checksum(ReadOnlySpan<byte> bytes)
    {
        byte checksum = 0;
        foreach (var value in bytes)
            checksum ^= value;
        return checksum;
    }

    protected static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);
}
