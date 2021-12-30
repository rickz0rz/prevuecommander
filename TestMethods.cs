using System.Text;

namespace PrevueCommander;

public static class TestMethods
{
    public static byte[] StringToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            .ToArray();
    }

    public static void ParseAndPrint(byte[] bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            try
            {
                if (bytes[i] == 0x55 && bytes[i + 1] == 0xAA)
                {
                    var stringBuilder = new StringBuilder();

                    i += 2;
                    var cs = 0x55 ^ 0xAA;

                    var commandName = string.Empty;

                    switch (bytes[i])
                    {
                        case 0x41: // A
                            commandName = "Address";

                            stringBuilder.AppendLine($"[{i:X4}] {commandName} ({bytes[i]:X2})");
                            cs ^= bytes[i];
                            i++;

                            // Up to 9 chars for the ID... maybe test that in a loop.
                            stringBuilder.Append($"[{i:X4}] {commandName} > Name: \"");
                            while (bytes[i] != 0x00)
                            {
                                stringBuilder.Append((char)bytes[i]);
                                cs ^= bytes[i];
                                i++;
                            }

                            stringBuilder.AppendLine("\"");

                            stringBuilder.Append($"[{i:X4}] {commandName} > Terminator ({bytes[i]:X2})");
                            i++;
                            break;
                        case 0x43: // C
                            commandName = "Channel Lineup Info";

                            stringBuilder.AppendLine($"[{i:X4}] {commandName} ({bytes[i]:X2})");
                            cs ^= bytes[i];
                            i++;

                            stringBuilder.AppendLine($"[{i:X4}] {commandName} > Julian Date ({bytes[i]:X2})");
                            cs ^= bytes[i];
                            i++;

                            var channelIndex = 0;
                            while (bytes[i] == 0x12)
                            {
                                stringBuilder.AppendLine(
                                    $"[{i:X4}] {commandName} > Channel[{channelIndex}] ({bytes[i]:X2})");
                                cs ^= bytes[i];
                                i++;

                                stringBuilder.AppendLine(
                                    $"[{i:X4}] {commandName} > Channel[{channelIndex}] > Source Attribute ({Convert.ToString(bytes[i], 2).PadLeft(8, '0')})");
                                cs ^= bytes[i];
                                i++;

                                stringBuilder.Append(
                                    $"[{i:X4}] {commandName} > Channel[{channelIndex}] > Source Name: \"");
                                while (bytes[i] != 0x00 && bytes[i] != 0x12 &&
                                       bytes[i] != 0x11 && bytes[i] != 0x14 &&
                                       bytes[i] != 0x01)
                                {
                                    stringBuilder.Append((char)bytes[i]);
                                    cs ^= bytes[i];
                                    i++;
                                }

                                stringBuilder.AppendLine("\"");

                                while (bytes[i] != 0x00 && bytes[i] != 0x12)
                                {
                                    switch (bytes[i])
                                    {
                                        case 0x11:
                                            // Channel number, up to 9 characters
                                            stringBuilder.Append(
                                                $"[{i:X4}] {commandName} > Channel[{channelIndex}] > Number: \"");
                                            i++;

                                            while (bytes[i] != 0x00 && bytes[i] != 0x12 &&
                                                   bytes[i] != 0x11 && bytes[i] != 0x14 &&
                                                   bytes[i] != 0x01)
                                            {
                                                stringBuilder.Append((char)bytes[i]);
                                                cs ^= bytes[i];
                                                i++;
                                            }

                                            stringBuilder.AppendLine("\"");
                                            break;
                                        case 0x14:
                                            // Time slot mask
                                            stringBuilder.Append(
                                                $"[{i:X4}] {commandName} > Channel[{channelIndex}] > Time slot mask: \"");
                                            i++;
                                            for (var _ = 0; _ < 6; _++)
                                            {
                                                stringBuilder.Append(bytes[i].ToString("X2"));
                                                cs ^= bytes[i];
                                                i++;
                                            }

                                            stringBuilder.AppendLine("\"");
                                            break;
                                        case 0x01:
                                            // Call letters
                                            stringBuilder.Append(
                                                $"[{i:X4}] {commandName} > Channel[{channelIndex}] > Call Letters: \"");
                                            i++;
                                            /*for (var cli = 0; cli < 6; cli++)
                                            {
                                                stringBuilder.Append((char)bytes[i]);
                                                cs ^= bytes[i];
                                                i++;
                                            }*/

                                            while (bytes[i] != 0x00 && bytes[i] != 0x12 &&
                                                   bytes[i] != 0x11 && bytes[i] != 0x14 &&
                                                   bytes[i] != 0x01)
                                            {
                                                stringBuilder.Append((char)bytes[i]);
                                                cs ^= bytes[i];
                                                i++;
                                            }

                                            stringBuilder.AppendLine("\"");
                                            break;
                                    }
                                }

                                channelIndex++;
                            }

                            stringBuilder.AppendLine($"[{i:X4}] {commandName} > Terminator ({bytes[i]:X2})");
                            i++;
                            break;
                        case 0x50: // P
                            commandName = "Program Info";

                            stringBuilder.AppendLine($"[{i:X4}] {commandName} ({bytes[i]:X2})");
                            cs ^= bytes[i];
                            i++;

                            stringBuilder.AppendLine(
                                $"[{i:X4}] {commandName} > Time Slot ({Convert.ToString(bytes[i], 2).PadLeft(8, '0')})");
                            cs ^= bytes[i];
                            i++;

                            stringBuilder.AppendLine($"[{i:X4}] {commandName} > Julian Date ({bytes[i]:X2})");
                            cs ^= bytes[i];
                            i++;

                            stringBuilder.Append(
                                $"[{i:X4}] {commandName} > Source Name: \"");
                            while (bytes[i] != 0x12)
                            {
                                stringBuilder.Append((char)bytes[i]);
                                cs ^= bytes[i];
                                i++;
                            }

                            stringBuilder.AppendLine("\"");

                            stringBuilder.AppendLine($"[{i:X4}] {commandName} > End of Source ({bytes[i]:X2})");
                            cs ^= bytes[i];
                            i++;

                            stringBuilder.AppendLine(
                                $"[{i:X4}] {commandName} > String Attribute ({Convert.ToString(bytes[i], 2).PadLeft(8, '0')})");
                            cs ^= bytes[i];
                            i++;

                            stringBuilder.Append($"[{i:X4}] {commandName} > Listing String: \"");
                            while (bytes[i] != 0x00)
                            {
                                switch (bytes[i])
                                {
                                    case 0x7C:
                                        stringBuilder.Append("[CC]");
                                        break;
                                    default:
                                        stringBuilder.Append((char)bytes[i]);
                                        break;
                                }

                                cs ^= bytes[i];
                                i++;
                            }

                            stringBuilder.AppendLine("\"");

                            stringBuilder.AppendLine($"[{i:X4}] {commandName} > Terminator ({bytes[i]:X2})");
                            i++;
                            break;
                        default:
                            throw new Exception($"Unhandled command code: {bytes[i]:X2}");
                    }

                    stringBuilder.AppendLine($"[{i:X4}] Checksum ({bytes[i]:X2}) [Calculated was {cs:X2}]");
                    Console.Write(stringBuilder.ToString());
                }
                else
                {
                    throw new Exception($"Invalid command start marker: {bytes[i]:X2}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{i:X4}] ERROR: {e.Message} [{e.StackTrace}]");
            }

        }
    }
}