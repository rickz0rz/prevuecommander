using PrevueCommander.Commands;

namespace PrevueCommander;

public static class Helpers
{
    public static byte GetJulianDate(DateTime dateTime)
    {
        var p = dateTime.DayOfYear - 1; // Start at zero?
        return (byte)((p >= 256) ? (p - 255) : p);
    }

    public static byte[] GuideFontTokenMapper(string token)
    {
        switch (token)
        {
            case "CC":
                return new byte[] { 0x7C };
            case "VCRPLUS":
                return new byte[] { 0x8E };
            case "DISNEY":
                return new byte[] { 0x92 };
            case "TVPG":
                return new byte[] { 0x9B };
            case "PREVUE":
                return new byte[] { 0x9E };
            default:
                Console.WriteLine($"Undefined token: {token}");
                return new[] { (byte)'?' };
        }
    }

    public static byte[] AdFontTokenMapper(string token)
    {
        // Notes: Some characters like | don't render
        // 40 characters per line
        switch (token)
        {
            case "COLOR":
                return new[] { (byte)0x03 };
            case "TRANSPARENT":
                return new[] { (byte)'0' };
            case "WHITE":
                return new[] { (byte)'1' };
            case "BLACK":
                return new[] { (byte)'2' };
            case "YELLOW":
                return new[] { (byte)'3' };
            case "RED":
                return new[] { (byte)'4' };
            case "CYAN":
                return new[] { (byte)'5' };
            case "GRAY":
            case "GREY":
                return new[] { (byte)'6' };
            case "BLUE":
                return new[] { (byte)'7' };
            case "LEFT":
                return new[] { (byte)0x19 };
            case "CENTER":
                return new[] { (byte)0x18 };
            case "RIGHT":
                return new[] { (byte)0x1A };
            default:
                Console.WriteLine($"Undefined token: {token}");
                return new[] { (byte)'?' };
        }
    }

    public static byte[] ConvertStringToBytes(string str, Func<string, byte[]> tokenMapper)
    {
        var bytes = new List<byte>();
        var chars = str.ToCharArray();

        var inTokenMode = false;
        var currentToken = string.Empty;

        for (var i = 0; i < chars.Length; i++)
        {
            switch (chars[i])
            {
                case '%': // Token start
                    if (inTokenMode)
                    {
                        inTokenMode = false;
                        if (!string.IsNullOrWhiteSpace(currentToken))
                        {
                            bytes.AddRange(tokenMapper(currentToken));
                            currentToken = string.Empty;
                        }
                    }
                    else
                    {
                        if (chars[i + 1] == '%')
                        {
                            bytes.Add((byte)chars[i]);
                            i++;
                        }
                        else
                        {
                            inTokenMode = true;
                        }
                    }

                    break;
                default:
                    if (inTokenMode)
                    {
                        currentToken += chars[i];
                    }
                    else
                    {
                        bytes.Add((byte)chars[i]);
                    }

                    break;
            }
        }

        /*
        // If we left off in the middle of a token..
        if (!string.IsNullOrWhiteSpace(currentToken))
        {
            bytes.AddRange(ConvertTokenToByte(currentToken));
        }
        */

        return bytes.ToArray();
    }

    public static List<LocalAdCommand> GenerateAdCommands(string[] ads)
    {
        var commands = new List<LocalAdCommand>();
        for (var i = 1; i <= ads.Length; i++)
        {
            commands.Add(new LocalAdCommand(i, ads[i - 1]));
        }
        return commands;
    }
}