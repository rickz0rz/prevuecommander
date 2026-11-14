using System.Globalization;
using System.Text;

namespace Prevue.Commands;

/// <summary>One command of a captured feed.</summary>
/// <param name="Offset">The position of the first byte of the command in the feed.</param>
/// <param name="Bytes">All the bytes of the command, with the preamble and the checksum.</param>
/// <param name="Name">The name of the command.</param>
/// <param name="ChecksumIsCorrect">True if the XOR of all the bytes is 0.</param>
/// <param name="Fields">The fields of the command, as a name and a value.</param>
public sealed record DecodedCommand(
    int Offset,
    byte[] Bytes,
    string Name,
    bool ChecksumIsCorrect,
    IReadOnlyList<(string Name, string Value)> Fields);

/// <summary>
/// Splits a captured feed into its commands and shows their fields, for example to compare the feed of Prevue Commander
/// with other feeds.
/// </summary>
/// <remarks>
/// A data line command starts with <c>55 AA</c> and ends with a checksum, the XOR of all the bytes before it. Thus the
/// XOR of all the bytes of a correct command is 0. A payload can have a NUL byte, for example the clock on a Sunday, so
/// the decoder ends a command at the first <c>55 AA</c> after which the XOR is 0. A control line packet ends with a CR
/// and its checksum.
/// </remarks>
public static class FeedDecoder
{
    private const byte PreambleStart = 0x55;
    private const byte PreambleEnd = 0xAA;
    private const byte ControlPacketEnd = 0x0D;

    /// <summary>The commands of a data line feed. Bytes before the first preamble are a command named "junk".</summary>
    public static IEnumerable<DecodedCommand> DecodeData(byte[] feed)
    {
        var start = NextPreamble(feed, 0);
        if (start != 0)
            yield return Junk(feed, 0, start < 0 ? feed.Length : start);

        while (start >= 0)
        {
            // Find the first end with a correct checksum. If no end is correct, the command ends at the next preamble.
            var firstEnd = -1;
            var end = -1;
            for (var next = NextPreamble(feed, start + 2); ; next = NextPreamble(feed, next + 2))
            {
                var candidate = next < 0 ? feed.Length : next;
                if (firstEnd < 0)
                    firstEnd = candidate;
                if (Xor(feed.AsSpan(start, candidate - start)) == 0)
                {
                    end = candidate;
                    break;
                }

                if (next < 0)
                    break;
            }

            var correct = end >= 0;
            if (!correct)
                end = firstEnd;

            var bytes = feed[start..end];
            var (name, fields) = DataFields(bytes);
            yield return new DecodedCommand(start, bytes, name, correct, fields);
            start = end < feed.Length ? end : -1;
        }
    }

    /// <summary>The packets of a control line feed.</summary>
    public static IEnumerable<DecodedCommand> DecodeControl(byte[] feed)
    {
        var start = 0;
        while (start < feed.Length)
        {
            // A packet ends with a CR and the checksum. A CR without a correct checksum after it can be in the body.
            var end = -1;
            for (var i = start + 1; i + 1 < feed.Length; i++)
            {
                if (feed[i] == ControlPacketEnd && Xor(feed.AsSpan(start, i + 2 - start)) == 0)
                {
                    end = i + 2;
                    break;
                }
            }

            var correct = end >= 0;
            if (!correct)
                end = feed.Length;

            var bytes = feed[start..end];
            var type = bytes[0];
            var bodyEnd = correct ? bytes.Length - 2 : bytes.Length;
            List<(string, string)> fields =
            [
                ("type", type.ToString(CultureInfo.InvariantCulture)),
                ("body", Text(bytes.AsSpan(1, Math.Max(0, bodyEnd - 1))))
            ];
            yield return new DecodedCommand(start, bytes, $"control packet {type}", correct, fields);
            start = end;
        }
    }

    /// <summary>Writes the commands as text, one command and its fields at a time.</summary>
    /// <returns>The number of commands with a bad checksum.</returns>
    public static int Write(IEnumerable<DecodedCommand> commands, TextWriter output, bool showBytes)
    {
        var errors = 0;
        foreach (var command in commands)
        {
            if (!command.ChecksumIsCorrect)
                errors++;

            output.WriteLine($"{command.Offset:X6}  {command.Name} ({command.Bytes.Length} bytes)" +
                             (command.ChecksumIsCorrect ? string.Empty : "  BAD CHECKSUM"));
            foreach (var (name, value) in command.Fields)
                output.WriteLine($"        {name}: {value}");
            if (showBytes)
                output.WriteLine($"        bytes: {Convert.ToHexString(command.Bytes)}");
        }

        return errors;
    }

    private static DecodedCommand Junk(byte[] feed, int start, int end) =>
        new(start, feed[start..end], "junk", false, [("note", "These bytes are before the first 55 AA.")]);

    private static int NextPreamble(byte[] feed, int from)
    {
        for (var i = from; i + 1 < feed.Length; i++)
        {
            if (feed[i] == PreambleStart && feed[i + 1] == PreambleEnd)
                return i;
        }

        return -1;
    }

    private static byte Xor(ReadOnlySpan<byte> bytes)
    {
        byte value = 0;
        foreach (var b in bytes)
            value ^= b;
        return value;
    }

    /// <summary>The name and the fields of a data line command.</summary>
    private static (string Name, List<(string, string)> Fields) DataFields(byte[] command)
    {
        if (command.Length < 4)
            return ("incomplete command", []);

        // The body is the bytes between the preamble and the checksum. Most commands end their payload with a NUL.
        var body = command.AsSpan(2, command.Length - 3);
        var code = body[0];
        var payload = body.Length > 1 && body[^1] == 0 ? body[1..^1] : body[1..];
        var fields = new List<(string, string)>();
        string name;
        switch ((char)code)
        {
            case 'A':
                name = "'A' address";
                fields.Add(("target", Text(payload)));
                break;
            case 'F':
                name = "'F' status packet";
                Status(payload, fields);
                break;
            case 'f':
                name = "'f' configuration record";
                if (payload.Length >= 3)
                {
                    fields.Add(("length", ((payload[1] << 8) | payload[2]).ToString(CultureInfo.InvariantCulture)));
                    fields.Add(("record", Text(payload[3..])));
                }

                break;
            case 'K':
                name = "'K' clock";
                Clock(payload, fields);
                break;
            case 'g':
                name = "'g' daylight saving time window";
                Window(body[1..], fields);
                break;
            case 'C':
                name = "'C' channel lineup";
                LineUp(payload, fields);
                break;
            case 'P':
                name = "'P' program";
                Program(payload, fields);
                break;
            case 'L' when payload is [0x92]:
                name = "'L' delete the local ads";
                break;
            case 'L':
                name = "'L' local ad";
                if (payload.Length > 0)
                {
                    fields.Add(("number", payload[0].ToString(CultureInfo.InvariantCulture)));
                    fields.Add(("text", Text(payload[1..])));
                }

                break;
            case 'H':
                // A block has a sequence number and the length of its data. The header has the target path.
                if (body.Length >= 3 && body[2] == body.Length - 3)
                {
                    name = "'H' file transfer block";
                    fields.Add(("sequence", body[1].ToString(CultureInfo.InvariantCulture)));
                    fields.Add(("length", body[2].ToString(CultureInfo.InvariantCulture)));
                }
                else
                {
                    name = "'H' file transfer";
                    fields.Add(("path", Text(payload)));
                }

                break;
            case 'x':
                name = "'x' utility";
                fields.Add(("text", Text(payload)));
                break;
            case 'I':
                name = "'I' weather label";
                if (payload.Length > 0)
                {
                    fields.Add(("digit", Text(payload[..1])));
                    fields.Add(("label and text", Text(payload[1..])));
                }

                break;
            case 'W':
                name = "'W' weather text";
                fields.Add(("payload", Text(payload)));
                break;
            case '%':
                name = "'%' save";
                break;
            case 'R':
                name = "'R' reset";
                break;
            default:
                name = code == 0xBB ? "box off" : $"unknown command {Code(code)}";
                if (code != 0xBB && payload.Length > 0)
                    fields.Add(("payload", Text(payload)));
                break;
        }

        return (name, fields);
    }

    private static void Status(ReadOnlySpan<byte> payload, List<(string, string)> fields)
    {
        if (payload.Length != 20)
        {
            fields.Add(("note", $"ESQ reads 20 bytes, but the payload has {payload.Length}."));
            fields.Add(("payload", Text(payload)));
            return;
        }

        fields.Add(("scroll speed", Text(payload[2..3])));
        fields.Add(("time zone", Text(payload[9..10])));
        fields.Add(("daylight saving time", Text(payload[10..11])));
        fields.Add(("clock offset minutes", payload[19].ToString(CultureInfo.InvariantCulture)));
        fields.Add(("all", Text(payload)));
    }

    private static void Clock(ReadOnlySpan<byte> payload, List<(string, string)> fields)
    {
        if (payload.Length < 8)
        {
            fields.Add(("payload", Text(payload)));
            return;
        }

        // The payload is the day of the week, the month - 1, the day - 1, the year - 1900, the time and a DST flag.
        var text = $"{payload[3] + 1900:D4}-{payload[1] + 1:D2}-{payload[2] + 1:D2} " +
                   $"{payload[4]:D2}:{payload[5]:D2}:{payload[6]:D2}";
        fields.Add(("time", text));
        fields.Add(("day of the week", ((DayOfWeek)(payload[0] % 7)).ToString()));
        fields.Add(("daylight saving time", payload[7] != 0 ? "yes" : "no"));
    }

    private static void Window(ReadOnlySpan<byte> payload, List<(string, string)> fields)
    {
        if (payload.Length == 0)
            return;

        fields.Add(("window", Text(payload[..1])));
        // After the window, the length as two digits, then $04 and the start, then $13 and the end.
        var rest = Encoding.Latin1.GetString(payload[1..]).TrimEnd('\0');
        var start = rest.IndexOf('\x04');
        var end = rest.IndexOf('\x13');
        if (start >= 0 && end > start)
        {
            fields.Add(("start", rest[(start + 1)..end]));
            fields.Add(("end", rest[(end + 1)..]));
        }
        else
        {
            fields.Add(("payload", Text(payload[1..])));
        }
    }

    private static void LineUp(ReadOnlySpan<byte> payload, List<(string, string)> fields)
    {
        if (payload.Length == 0)
            return;

        fields.Add(("group code", payload[0].ToString(CultureInfo.InvariantCulture)));
        // Each channel: $12, the attribute, the source name, $11 and the number, $14 and the slot mask, $01 and the
        // call letters.
        var count = 0;
        var i = 1;
        while (i < payload.Length && payload[i] == 0x12)
        {
            var attribute = i + 1 < payload.Length ? payload[i + 1] : (byte)0;
            var sourceEnd = IndexOf(payload, 0x11, i + 2);
            var numberEnd = IndexOf(payload, 0x14, sourceEnd + 1);
            var callStart = numberEnd + 7;
            if (sourceEnd < 0 || numberEnd < 0 || callStart >= payload.Length || payload[callStart] != 0x01)
                break;

            var callEnd = IndexOf(payload, 0x12, callStart + 1);
            if (callEnd < 0)
                callEnd = payload.Length;

            count++;
            var mask = Convert.ToHexString(payload[(numberEnd + 1)..callStart]);
            fields.Add(($"channel {count}",
                $"source {Text(payload[(i + 2)..sourceEnd])}, number {Text(payload[(sourceEnd + 1)..numberEnd])}, " +
                $"call letters {Text(payload[(callStart + 1)..callEnd])}, attribute ${attribute:X2}, slots {mask}"));
            i = callEnd;
        }

        if (i < payload.Length)
            fields.Add(("rest", Text(payload[i..])));
    }

    private static void Program(ReadOnlySpan<byte> payload, List<(string, string)> fields)
    {
        var sourceEnd = IndexOf(payload, 0x12, 2);
        if (payload.Length < 2 || sourceEnd < 0 || sourceEnd + 1 >= payload.Length)
        {
            fields.Add(("payload", Text(payload)));
            return;
        }

        fields.Add(("slot", payload[0].ToString(CultureInfo.InvariantCulture)));
        fields.Add(("group code", payload[1].ToString(CultureInfo.InvariantCulture)));
        fields.Add(("source", Text(payload[2..sourceEnd])));
        fields.Add(("attribute", $"${payload[sourceEnd + 1]:X2}" + ((payload[sourceEnd + 1] & 0x02) != 0 ? " (movie)" : "")));
        fields.Add(("text", Text(payload[(sourceEnd + 2)..])));
    }

    private static int IndexOf(ReadOnlySpan<byte> bytes, byte value, int from)
    {
        if (from < 0 || from > bytes.Length)
            return -1;
        var index = bytes[from..].IndexOf(value);
        return index < 0 ? -1 : from + index;
    }

    private static string Code(byte code) =>
        code is >= 0x20 and < 0x7F ? $"'{(char)code}'" : $"${code:X2}";

    /// <summary>The bytes as Latin-1 text, with the control bytes as escapes, for example \x12.</summary>
    private static string Text(ReadOnlySpan<byte> bytes)
    {
        var text = new StringBuilder("\"");
        foreach (var b in bytes)
        {
            if (b is < 0x20 or (>= 0x7F and < 0xA0) || b == '"' || b == '\\')
                text.Append(b is (byte)'"' or (byte)'\\' ? $"\\{(char)b}" : $"\\x{b:X2}");
            else
                text.Append((char)b);
        }

        return text.Append('"').ToString();
    }
}
