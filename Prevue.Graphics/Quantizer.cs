namespace Prevue.Graphics;

/// <summary>
/// Changes a true-color image into a picture with few Amiga colors: it scales the image, chooses the colors with a
/// median cut, and dithers the pixels.
/// </summary>
internal static class Quantizer
{
    /// <summary>Scales an image to a size, with the average of the source pixels that each pixel covers.</summary>
    internal static byte[] Scale(RgbaImage image, int width, int height)
    {
        var result = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            var y0 = y * image.Height / height;
            var y1 = Math.Max(y0 + 1, (y + 1) * image.Height / height);
            for (var x = 0; x < width; x++)
            {
                var x0 = x * image.Width / width;
                var x1 = Math.Max(x0 + 1, (x + 1) * image.Width / width);
                // Colors are weighted by their alpha, so that transparent pixels do not darken the edges.
                double r = 0, g = 0, b = 0, a = 0;
                var count = 0;
                for (var sy = y0; sy < y1; sy++)
                {
                    for (var sx = x0; sx < x1; sx++)
                    {
                        var i = (sy * image.Width + sx) * 4;
                        var alpha = image.Pixels[i + 3];
                        r += image.Pixels[i] * alpha;
                        g += image.Pixels[i + 1] * alpha;
                        b += image.Pixels[i + 2] * alpha;
                        a += alpha;
                        count++;
                    }
                }

                var target = (y * width + x) * 4;
                if (a > 0)
                    (result[target], result[target + 1], result[target + 2]) =
                        ((byte)(r / a), (byte)(g / a), (byte)(b / a));
                result[target + 3] = (byte)(a / count);
            }
        }

        return result;
    }

    /// <summary>
    /// Chooses up to a number of colors for the pixels (8-bit RGB triples) with a median cut: the box of colors with
    /// the most pixels is cut in two at the median of its widest part, until there are enough boxes.
    /// </summary>
    internal static List<AmigaColor> MedianCut(int[] rgb, int count)
    {
        var colors = new List<(int R, int G, int B)>(rgb.Length / 3);
        for (var i = 0; i < rgb.Length; i += 3)
            colors.Add((rgb[i], rgb[i + 1], rgb[i + 2]));
        var boxes = new List<List<(int R, int G, int B)>> { colors };
        while (boxes.Count < count)
        {
            var box = boxes.Where(b => b.Count > 1 && Range(b) > 0).OrderByDescending(b => b.Count * Range(b))
                .FirstOrDefault();
            if (box == null)
                break;
            var (dr, dg, db) = (Spread(box, c => c.R), Spread(box, c => c.G), Spread(box, c => c.B));
            Func<(int R, int G, int B), int> key = dr >= dg && dr >= db ? c => c.R : dg >= db ? c => c.G : c => c.B;
            var sorted = box.OrderBy(key).ToList();
            boxes.Remove(box);
            boxes.Add(sorted[..(sorted.Count / 2)]);
            boxes.Add(sorted[(sorted.Count / 2)..]);
        }

        return boxes.Where(b => b.Count > 0).Select(b => AmigaColor.From(
                (int)b.Average(c => c.R), (int)b.Average(c => c.G), (int)b.Average(c => c.B)))
            .Distinct().ToList();

        static int Spread(List<(int R, int G, int B)> box, Func<(int R, int G, int B), int> part) =>
            box.Max(part) - box.Min(part);
        static int Range(List<(int R, int G, int B)> box) =>
            Math.Max(Spread(box, c => c.R), Math.Max(Spread(box, c => c.G), Spread(box, c => c.B)));
    }

    /// <summary>
    /// Changes each pixel to the nearest color of the palette (not color 0, the genlock key), with Floyd-Steinberg
    /// dithering. The first part of each pixel gets the palette index.
    /// </summary>
    /// <param name="key">The pixels that get <paramref name="keyIndex"/>. They do not get or give an error.</param>
    /// <param name="keyIndex">The color of the key pixels: color 0, the genlock key, or the background.</param>
    internal static void Dither(int[] rgb, int width, int height, List<AmigaColor> palette, bool[]? key = null,
        int keyIndex = 0)
    {
        var values = palette.Select(c => c.Rgb).ToArray();
        var error = new double[rgb.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 3;
                if (key != null && key[y * width + x])
                {
                    rgb[i] = keyIndex;
                    continue;
                }

                var r = Math.Clamp(rgb[i] + error[i], 0, 255);
                var g = Math.Clamp(rgb[i + 1] + error[i + 1], 0, 255);
                var b = Math.Clamp(rgb[i + 2] + error[i + 2], 0, 255);
                var best = 1;
                var bestDistance = double.MaxValue;
                for (var p = 1; p < values.Length; p++)
                {
                    var (pr, pg, pb) = values[p];
                    var distance = (r - pr) * (r - pr) * 0.3 + (g - pg) * (g - pg) * 0.59 + (b - pb) * (b - pb) * 0.11;
                    if (distance < bestDistance)
                        (best, bestDistance) = (p, distance);
                }

                var (cr, cg, cb) = values[best];
                Spread(x + 1, y, 7 / 16.0);
                Spread(x - 1, y + 1, 3 / 16.0);
                Spread(x, y + 1, 5 / 16.0);
                Spread(x + 1, y + 1, 1 / 16.0);
                rgb[i] = best;

                void Spread(int sx, int sy, double part)
                {
                    if (sx < 0 || sx >= width || sy >= height || (key != null && key[sy * width + sx]))
                        return;
                    var j = (sy * width + sx) * 3;
                    error[j] += (r - cr) * part;
                    error[j + 1] += (g - cg) * part;
                    error[j + 2] += (b - cb) * part;
                }
            }
        }
    }
}
