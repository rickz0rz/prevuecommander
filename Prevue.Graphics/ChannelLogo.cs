namespace Prevue.Graphics;

/// <summary>
/// Makes the picture of a channel logo for ESQ from an image of the station: a high-resolution picture of 640 by 240
/// pixels with 16 colors. The station image is on a light card at the left, on the navy of Prevue. The right part
/// stays empty, because ESQ writes the call letters and the channel number there.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>
/// Color 0 is the navy background, as the background of the logos of the drive is their color 0. Color 0 is the
/// genlock key, and ESQ shows the logo in a display window of 320 low-resolution pixels. So the navy of the logo and
/// the border around it are the same: the video of the genlock shows there, or the navy without a genlock video. A
/// Prevue machine without a genlock video showed the navy across the full width in the same way. The card and the
/// station image do not use color 0, so that the video does not show through them.
/// </item>
/// <item>
/// ESQ writes its text in the brightest color, so color 1 is white. ESQ writes a shadow under the text in color 2, so
/// color 2 is also navy: the text then has no shadow, as on a Prevue machine.
/// </item>
/// <item>
/// ESQ shows a logo, and writes its text, in the display mode of the picture. A low-resolution channel logo with 32
/// colors also works, but its text is two times as wide, in a font that is not pleasant to read. The channel logos of
/// Prevue machines in the field had high resolution and fewer colors, so a channel logo here is in high resolution,
/// with the 16 colors of that mode.
/// </item>
/// <item>
/// A high-resolution pixel is taller than it is wide: 22 to 26, as in the brushes of the drive. The station image is
/// wider in pixels than in its file, so that it has its shape on the screen.
/// </item>
/// </list>
/// </remarks>
public static class ChannelLogo
{
    public const int Width = 640, Height = 240, Planes = 4, Colors = 1 << Planes;

    private const double PixelAspect = 22.0 / 26.0;
    private static readonly AmigaColor White = new(15, 15, 15);
    private static readonly AmigaColor Navy = new(0, 0, 4);
    private static readonly AmigaColor Card = new(14, 14, 14);

    // The card on the left, in pixels of the logo, and the margin of the station image in it.
    private const int CardLeft = 24, CardTop = 30, CardRight = 260, CardBottom = 210, MarginX = 16, MarginY = 8;

    /// <summary>Makes the ILBM file of the channel logo of a station image.</summary>
    public static byte[] Create(RgbaImage station)
    {
        var (palette, pixels) = Render(station);
        return IlbmImage.Write(Width, Height, Planes, palette, pixels, IlbmFormat.HighResolution);
    }

    /// <summary>The palette and the palette index of each pixel of the channel logo.</summary>
    public static (List<AmigaColor> Palette, byte[] Pixels) Render(RgbaImage station)
    {
        // The card with the station image in its middle, in 8-bit RGB.
        var cardWidth = CardRight - CardLeft;
        var cardHeight = CardBottom - CardTop;
        var card = new int[cardWidth * cardHeight * 3];
        var (cardR, cardG, cardB) = Card.Rgb;
        for (var i = 0; i < cardWidth * cardHeight; i++)
            (card[i * 3], card[i * 3 + 1], card[i * 3 + 2]) = (cardR, cardG, cardB);

        // The image fits in the card without the margin, with its shape on the screen.
        var boxWidth = (cardWidth - 2 * MarginX) * PixelAspect;
        var boxHeight = (double)(cardHeight - 2 * MarginY);
        var scale = Math.Min(boxWidth / station.Width, boxHeight / station.Height);
        var imageWidth = Math.Max(1, (int)Math.Round(station.Width * scale / PixelAspect));
        var imageHeight = Math.Max(1, (int)Math.Round(station.Height * scale));
        var left = (cardWidth - imageWidth) / 2;
        var top = (cardHeight - imageHeight) / 2;
        var scaled = Quantizer.Scale(station, imageWidth, imageHeight);
        for (var y = 0; y < imageHeight; y++)
        {
            for (var x = 0; x < imageWidth; x++)
            {
                var source = (y * imageWidth + x) * 4;
                var alpha = scaled[source + 3] / 255.0;
                var target = ((top + y) * cardWidth + left + x) * 3;
                for (var c = 0; c < 3; c++)
                    card[target + c] = (int)Math.Round(scaled[source + c] * alpha + card[target + c] * (1 - alpha));
            }
        }

        var palette = new List<AmigaColor> { Navy, White, Navy, Card };
        foreach (var color in Quantizer.MedianCut(card, Colors - palette.Count))
        {
            if (!palette.Contains(color))
                palette.Add(color);
        }

        while (palette.Count < Colors)
            palette.Add(Navy);

        // The navy background is color 0.
        var pixels = new byte[Width * Height];
        Quantizer.Dither(card, cardWidth, cardHeight, palette);
        for (var y = 0; y < cardHeight; y++)
        {
            for (var x = 0; x < cardWidth; x++)
                pixels[(CardTop + y) * Width + CardLeft + x] = (byte)card[(y * cardWidth + x) * 3];
        }

        return (palette, pixels);
    }
}
