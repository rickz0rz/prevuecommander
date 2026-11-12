using System.Buffers.Binary;
using System.Text;

namespace Prevue.Graphics;

/// <summary>
/// The display format of an ILBM picture: the aspect of its pixels, the size of its page and its display mode
/// (CAMG), as the pictures of the Prevue drive have them.
/// </summary>
public sealed record IlbmFormat(byte XAspect, byte YAspect, short PageWidth, short PageHeight, uint DisplayMode)
{
    /// <summary>Low resolution, as the logos of the drive (Logos/INSIDER.UV): 320 pixels across.</summary>
    public static readonly IlbmFormat LowResolution = new(44, 26, 320, 400, 0x11004);

    /// <summary>High resolution, as the brushes of the drive (Brushes/TVGAT.UV): 640 pixels across.</summary>
    public static readonly IlbmFormat HighResolution = new(22, 26, 640, 400, 0x19004);
}

/// <summary>A color of the Amiga: 4 bits for red, green and blue (0 to 15).</summary>
public readonly record struct AmigaColor(byte R, byte G, byte B)
{
    /// <summary>The nearest Amiga color of a color with 8 bits for each part.</summary>
    public static AmigaColor From(int r, int g, int b) => new(Nibble(r), Nibble(g), Nibble(b));

    private static byte Nibble(int value) => (byte)Math.Clamp((value * 15 + 127) / 255, 0, 15);

    /// <summary>The parts with 8 bits (0 to 255).</summary>
    public (int R, int G, int B) Rgb => (R * 17, G * 17, B * 17);

    /// <summary>Reads a color as "#RRGGBB" or "#RGB", and gives the nearest Amiga color.</summary>
    /// <exception cref="FormatException">The text is not a color.</exception>
    public static AmigaColor Parse(string text)
    {
        var hex = text.TrimStart('#');
        if (hex.Length == 3)
            hex = string.Concat(hex.Select(c => new string(c, 2)));
        if (hex.Length != 6 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var value))
            throw new FormatException($"\"{text}\" is not a color. Use \"#RRGGBB\", for example \"#000044\".");
        return From(value >> 16, (value >> 8) & 0xFF, value & 0xFF);
    }
}

/// <summary>
/// Writes IFF ILBM pictures as ESQ reads them for its logos: bit planes with ByteRun1 compression, a palette, and the
/// values of the logos of the Prevue drive (the aspect of the pixels, the page, the hot spot and the display mode).
/// </summary>
public static class IlbmImage
{
    private const byte MaskTransparentColor = 2, CompressionByteRun1 = 1;

    /// <summary>Makes an ILBM file of a picture of palette indexes, with 2 to the power of the planes colors.</summary>
    /// <param name="pixels">The palette index of each pixel, row by row. The width must be a multiple of 16.</param>
    public static byte[] Write(int width, int height, int planes, IReadOnlyList<AmigaColor> palette, byte[] pixels,
        IlbmFormat? format = null)
    {
        format ??= IlbmFormat.LowResolution;
        if (width % 16 != 0)
            throw new ArgumentException("The width of an ILBM picture here must be a multiple of 16.", nameof(width));
        var colors = 1 << planes;
        using var body = new MemoryStream();
        var rowBytes = width / 8;
        var plane = new byte[rowBytes];
        for (var y = 0; y < height; y++)
        {
            for (var p = 0; p < planes; p++)
            {
                Array.Clear(plane);
                for (var x = 0; x < width; x++)
                {
                    if ((pixels[y * width + x] >> p & 1) != 0)
                        plane[x / 8] |= (byte)(0x80 >> (x % 8));
                }

                ByteRun1(plane, body);
            }
        }

        using var form = new MemoryStream();
        var header = new byte[20];
        BinaryPrimitives.WriteUInt16BigEndian(header, (ushort)width);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(2), (ushort)height);
        header[8] = (byte)planes;
        header[9] = MaskTransparentColor;
        header[10] = CompressionByteRun1;
        header[14] = format.XAspect;
        header[15] = format.YAspect;
        BinaryPrimitives.WriteInt16BigEndian(header.AsSpan(16), format.PageWidth);
        BinaryPrimitives.WriteInt16BigEndian(header.AsSpan(18), format.PageHeight);
        Chunk(form, "BMHD", header);

        var map = new byte[colors * 3];
        for (var i = 0; i < colors && i < palette.Count; i++)
            (map[i * 3], map[i * 3 + 1], map[i * 3 + 2]) = ((byte)palette[i].Rgb.R, (byte)palette[i].Rgb.G, (byte)palette[i].Rgb.B);
        Chunk(form, "CMAP", map);

        var grab = new byte[4];
        BinaryPrimitives.WriteInt16BigEndian(grab, (short)(width / 2));
        BinaryPrimitives.WriteInt16BigEndian(grab.AsSpan(2), (short)(height / 2));
        Chunk(form, "GRAB", grab);

        var mode = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(mode, format.DisplayMode);
        Chunk(form, "CAMG", mode);
        Chunk(form, "BODY", body.ToArray());

        using var file = new MemoryStream();
        file.Write("FORM"u8);
        var size = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(size, (uint)(form.Length + 4));
        file.Write(size);
        file.Write("ILBM"u8);
        form.WriteTo(file);
        return file.ToArray();
    }

    private static void Chunk(Stream output, string id, byte[] data)
    {
        output.Write(Encoding.ASCII.GetBytes(id));
        var size = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(size, (uint)data.Length);
        output.Write(size);
        output.Write(data);
        if (data.Length % 2 != 0)
            output.WriteByte(0);
    }

    /// <summary>
    /// Packs a row with ByteRun1: n (0 to 127) copies the next n + 1 bytes, and -n (1 to 127) repeats the next byte
    /// n + 1 times.
    /// </summary>
    public static void ByteRun1(ReadOnlySpan<byte> row, Stream output)
    {
        var i = 0;
        while (i < row.Length)
        {
            var run = 1;
            while (i + run < row.Length && run < 128 && row[i + run] == row[i])
                run++;
            if (run >= 2)
            {
                output.WriteByte((byte)(sbyte)(1 - run));
                output.WriteByte(row[i]);
                i += run;
                continue;
            }

            // A literal ends before a run of 2 or more of the same byte.
            var start = i;
            while (i < row.Length && i - start < 128 &&
                   !(i + 1 < row.Length && row[i + 1] == row[i]))
                i++;
            if (i == start)
                i++;
            output.WriteByte((byte)(i - start - 1));
            output.Write(row[start..i]);
        }
    }
}
