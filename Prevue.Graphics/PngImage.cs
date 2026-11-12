using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Prevue.Graphics;

/// <summary>An image in RGBA: 4 bytes for each pixel, from the top left, row by row.</summary>
public sealed record RgbaImage(int Width, int Height, byte[] Pixels);

/// <summary>Reads PNG files: all the color types, the bit depths, and transparency, without interlace.</summary>
public static class PngImage
{
    private static readonly byte[] Signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>True if the data starts as a PNG file.</summary>
    public static bool IsPng(ReadOnlySpan<byte> data) => data.StartsWith(Signature);

    /// <summary>Reads a PNG file.</summary>
    /// <exception cref="InvalidDataException">The data is not a PNG file that this reader can read.</exception>
    public static RgbaImage Read(byte[] data)
    {
        if (!IsPng(data))
            throw new InvalidDataException("The file is not a PNG file.");
        int width = 0, height = 0, depth = 0, colorType = 0;
        byte[] palette = [];
        byte[] transparency = [];
        using var compressed = new MemoryStream();
        var position = Signature.Length;
        while (position + 8 <= data.Length)
        {
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position));
            var type = Encoding.ASCII.GetString(data, position + 4, 4);
            if (position + 12 + length > data.Length)
                throw new InvalidDataException("The PNG file ends in a chunk.");
            var chunk = data.AsSpan(position + 8, length);
            switch (type)
            {
                case "IHDR":
                    width = (int)BinaryPrimitives.ReadUInt32BigEndian(chunk);
                    height = (int)BinaryPrimitives.ReadUInt32BigEndian(chunk[4..]);
                    depth = chunk[8];
                    colorType = chunk[9];
                    if (chunk[12] != 0)
                        throw new InvalidDataException("The PNG file is interlaced. Save it without interlace.");
                    break;
                case "PLTE":
                    palette = chunk.ToArray();
                    break;
                case "tRNS":
                    transparency = chunk.ToArray();
                    break;
                case "IDAT":
                    compressed.Write(chunk);
                    break;
            }

            if (type == "IEND")
                break;
            position += 12 + length;
        }

        // An image can be long and low: for example 12576 by 75 pixels.
        if (width <= 0 || height <= 0 || width > 65535 || height > 65535 || (long)width * height > 64 << 20)
            throw new InvalidDataException("The PNG file has no size, or it is too large.");
        var channels = colorType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => throw new InvalidDataException($"The PNG color type {colorType} is not known."),
        };
        var bitsPerPixel = channels * depth;
        var stride = (width * bitsPerPixel + 7) / 8;
        var bytesPerPixel = Math.Max(1, bitsPerPixel / 8);
        var raw = new byte[(stride + 1) * height];
        compressed.Position = 0;
        using (var zlib = new ZLibStream(compressed, CompressionMode.Decompress))
            zlib.ReadExactly(raw);

        var result = new byte[width * height * 4];
        var previous = new byte[stride];
        var current = new byte[stride];
        for (var y = 0; y < height; y++)
        {
            var filter = raw[y * (stride + 1)];
            raw.AsSpan(y * (stride + 1) + 1, stride).CopyTo(current);
            Unfilter(filter, current, previous, bytesPerPixel);
            for (var x = 0; x < width; x++)
                WritePixel(result.AsSpan((y * width + x) * 4, 4), current, x, colorType, depth, palette, transparency);
            (previous, current) = (current, previous);
        }

        return new RgbaImage(width, height, result);
    }

    private static void Unfilter(byte filter, byte[] line, byte[] previous, int bytesPerPixel)
    {
        for (var i = 0; i < line.Length; i++)
        {
            int left = i >= bytesPerPixel ? line[i - bytesPerPixel] : 0;
            int up = previous[i];
            int upLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : 0;
            line[i] += filter switch
            {
                0 => 0,
                1 => (byte)left,
                2 => (byte)up,
                3 => (byte)((left + up) / 2),
                4 => (byte)Paeth(left, up, upLeft),
                _ => throw new InvalidDataException($"The PNG filter {filter} is not known."),
            };
        }
    }

    private static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    /// <summary>Reads one sample of a line: 1, 2, 4, 8 or 16 bits, as 8 bits.</summary>
    private static int Sample(byte[] line, int index, int depth)
    {
        switch (depth)
        {
            case 8:
                return line[index];
            case 16:
                return line[index * 2];
            default:
                var bit = index * depth;
                var value = (line[bit / 8] >> (8 - depth - bit % 8)) & ((1 << depth) - 1);
                return value;
        }
    }

    private static void WritePixel(Span<byte> target, byte[] line, int x, int colorType, int depth, byte[] palette,
        byte[] transparency)
    {
        // Gray values of 1, 2 and 4 bits become 8 bits.
        int Scale(int value) => depth < 8 ? value * 255 / ((1 << depth) - 1) : value;
        switch (colorType)
        {
            case 0:
            {
                var raw = Sample(line, x, depth);
                var gray = (byte)Scale(raw);
                var transparent = transparency.Length >= 2 &&
                                  raw == BinaryPrimitives.ReadUInt16BigEndian(transparency) >> (depth == 16 ? 8 : 0);
                target[0] = target[1] = target[2] = gray;
                target[3] = transparent ? (byte)0 : (byte)255;
                break;
            }
            case 2:
                target[0] = (byte)Sample(line, x * 3, depth);
                target[1] = (byte)Sample(line, x * 3 + 1, depth);
                target[2] = (byte)Sample(line, x * 3 + 2, depth);
                target[3] = 255;
                break;
            case 3:
            {
                var index = Sample(line, x, depth);
                if (index * 3 + 2 < palette.Length)
                    palette.AsSpan(index * 3, 3).CopyTo(target);
                target[3] = index < transparency.Length ? transparency[index] : (byte)255;
                break;
            }
            case 4:
                target[0] = target[1] = target[2] = (byte)Sample(line, x * 2, depth);
                target[3] = (byte)Sample(line, x * 2 + 1, depth);
                break;
            case 6:
                target[0] = (byte)Sample(line, x * 4, depth);
                target[1] = (byte)Sample(line, x * 4 + 1, depth);
                target[2] = (byte)Sample(line, x * 4 + 2, depth);
                target[3] = (byte)Sample(line, x * 4 + 3, depth);
                break;
        }
    }
}
