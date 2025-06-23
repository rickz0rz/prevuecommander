namespace Prevue.Core;

public static class Helpers
{
    public static byte GetJulianDate(DateTime dateTime)
    {
        // Do more testing on this with dates past 255 days
        var p = dateTime.DayOfYear;
        return (byte)((p >= 256) ? (p - 255) : p);
    }

    public static byte[] GuideFontTokenMapper(string token)
    {
        return token switch
        {
            "CC" => new byte[] { 0x7C },
            "STEREO" => new byte[] { 0x91 },
            "VCRPLUS" => new byte[] { 0x8E },
            "DISNEY" => new byte[] { 0x92 },
            "R" => new byte[] { 0x84 },
            "PG" => new byte[] { 0x85 },
            "PG13" => new byte[] { 0x87 },
            "TVY" => new byte[] { 0x90 },
            "TVY7" => new byte[] { 0x93 },
            "NC17" => new byte[] { 0x8F },
            "TVG" => new byte[] { 0x99 },
            "TV14" => new byte[] { 0x9A },
            "TVPG" => new byte[] { 0x9B },
            "TVM" => new byte[] { 0xA1 },
            "TVMA" => new byte[] { 0xA3 },
            "PREVUE" => new byte[] { 0x9E },
            _ => new[] { (byte)'?' }
        };
    }

    public static byte[] AdFontTokenMapper(string token)
    {
        // Notes: Some characters like | don't render
        // 40 characters per line
        return token switch
        {
            "COLOR" => new[] { (byte)0x03 },
            "TRANSPARENT" => new[] { (byte)'0' },
            "WHITE" => new[] { (byte)'1' },
            "BLACK" => new[] { (byte)'2' },
            "YELLOW" => new[] { (byte)'3' },
            "RED" => new[] { (byte)'4' },
            "CYAN" => new[] { (byte)'5' },
            "GRAY" => new[] { (byte)'6' },
            "GREY" => new[] { (byte)'6' },
            "BLUE" => new[] { (byte)'7' },
            "LEFT" => new[] { (byte)0x19 },
            "CENTER" => new[] { (byte)0x18 },
            "RIGHT" => new[] { (byte)0x1A },
            _ => new[] { (byte)'?' }
        };
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
                        if (string.IsNullOrWhiteSpace(currentToken))
                        {
                            bytes.AddRange(new[] { (byte)'%'});
                        }
                        else
                        {
                            bytes.AddRange(tokenMapper(currentToken));
                        }

                        currentToken = string.Empty;
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
}
