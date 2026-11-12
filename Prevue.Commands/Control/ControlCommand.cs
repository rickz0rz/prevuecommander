namespace Prevue.Commands.Control;

/// <summary>
/// A packet of the 110 baud control line of ESQ (CTRL). ESQ reads a packet as a type from 1 to 22, the body, a CR and
/// a checksum. The checksum is the XOR of all the bytes before it.
/// </summary>
/// <remarks>
/// ESQ counts a packet of more than 198 bytes of type, body and CR as too long
/// (<c>_SCRIPT_CtrlCmdLengthErrorCount</c>), and it ignores a packet with a bad checksum.
/// </remarks>
public abstract class ControlCommand : BaseCommand
{
    public const int MaximumBodyLength = 196;

    /// <summary>The separator of the right and the left box in a body.</summary>
    public const char Separator = '\x12';

    private const byte PacketEnd = 0x0D;

    protected ControlCommand(int type)
    {
        if (type is < 1 or > 22)
            throw new ArgumentOutOfRangeException(nameof(type), "A control line packet has a type from 1 to 22.");
        Type = type;
    }

    public int Type { get; }

    public override SerialLine Line => SerialLine.Control;

    /// <summary>The text between the type and the CR.</summary>
    protected abstract string Body { get; }

    protected sealed override byte[] GetPayload()
    {
        var body = Body;
        if (body.Length > MaximumBodyLength)
            throw new ArgumentException(
                $"The body of a control line packet must have {MaximumBodyLength} characters or less: {body}");
        if (body.Any(c => c > 0xFF || c == PacketEnd))
            throw new ArgumentException($"The body of a control line packet must have Latin-1 characters only, and no CR: {body}");
        return body.Select(c => (byte)c).ToArray();
    }

    public sealed override byte[] Render()
    {
        byte[] packet = [(byte)Type, .. GetPayload(), PacketEnd, 0x00];
        packet[^1] = Checksum(packet.AsSpan(0, packet.Length - 1));
        return packet;
    }

    public override string ToString() => $"{GetType().Name} {Type} \"{Body.Replace(Separator, '|')}\"";
}

/// <summary>Any packet of the control line, for the types and sub-commands that have no class.</summary>
public sealed class RawControlCommand(int type, string body) : ControlCommand(type)
{
    protected override string Body => body;
}
