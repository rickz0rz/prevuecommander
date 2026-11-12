namespace Prevue.Graphics;

/// <summary>How an image fills the picture when their shapes are not the same.</summary>
public enum PictureFit
{
    /// <summary>All the image shows. The background fills the rest of the picture.</summary>
    Contain,

    /// <summary>The image fills all the picture. The edges of the image that are outside the picture do not show.</summary>
    Cover,

    /// <summary>The image fills all the picture, and its shape changes.</summary>
    Stretch,
}

/// <summary>
/// The size, the colors and the display mode of a picture for ESQ.
/// </summary>
/// <param name="PixelAspect">The width of a pixel on the screen, divided by its height.</param>
/// <param name="FixedColors">The first colors of the palette, which ESQ does not take from the picture.</param>
/// <param name="BackgroundIndex">The color of the background, when it is not the genlock key (color 0).</param>
/// <param name="VisibleHeight">The rows that ESQ shows. The image goes in these rows, and the background fills the
/// other rows.</param>
public sealed record PictureLayout(string Name, int Width, int Height, int Planes, IlbmFormat Format,
    double PixelAspect, IReadOnlyList<AmigaColor>? FixedColors = null, int BackgroundIndex = 0, int? VisibleHeight = null)
{
    /// <summary>ESQ shows a maximum of 220 rows of a logo. It cuts the rows below them.</summary>
    public const int LogoRows = 220;

    /// <summary>The colors 0 to 3 of the grid, as ESQ shows them in the rows of the banner.</summary>
    public static readonly IReadOnlyList<AmigaColor> GridColors =
        [new(0, 0, 3), new(10, 10, 10), new(1, 1, 1), new(12, 12, 0)];

    /// <summary>
    /// A logo in low resolution, as the logos of the drive (Logos/INSIDER.UV): 320 by 240, 32 colors. ESQ shows 220
    /// rows of it.
    /// </summary>
    public static readonly PictureLayout LowResolution =
        new("low", 320, 240, 5, IlbmFormat.LowResolution, 44.0 / 26.0, VisibleHeight: LogoRows);

    /// <summary>A logo in high resolution, as the channel logos: 640 by 240, 16 colors. ESQ shows 220 rows of it.</summary>
    public static readonly PictureLayout HighResolution =
        new("high", 640, 240, 4, IlbmFormat.HighResolution, 22.0 / 26.0, VisibleHeight: LogoRows);

    /// <summary>
    /// The banner between the rows of the grid, as Banner/TVGBANNR.UV: 640 by 160, high resolution, 8 colors (3
    /// planes). ESQ shows only 3 planes. It takes only colors 4 to 7 of the picture: colors 0 to 3 are the colors of
    /// the grid. ESQ fills the row around the picture with color 7, so color 7 is the background, as in TVGBANNR.UV.
    /// </summary>
    public static readonly PictureLayout Banner =
        new("banner", 640, 160, 3, IlbmFormat.HighResolution, 22.0 / 26.0, GridColors, BackgroundIndex: 7);

    /// <summary>The highest banner. ESQ does not limit the height of the banner. A test showed 240 rows.</summary>
    public const int MaximumBannerHeight = 240;

    public int Colors => 1 << Planes;
}

/// <summary>
/// Makes a picture for ESQ from an image of any size: an ad (a general logo of LOGO.LST) or the banner.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>
/// Color 0 is the genlock key: the genlock video shows through it. The transparent pixels of the image show the
/// background. With <c>keyBackground</c>, the background is color 0. The pixels of the image do not use color 0, so
/// that the video does not show through them.
/// </item>
/// <item>
/// The pixels of the Amiga are not square. The image gets the shape that it has on the screen: for example, a
/// low-resolution pixel is wider than it is high.
/// </item>
/// </list>
/// </remarks>
public static class AdPicture
{
    /// <summary>The largest difference (8-bit) of each part of a color that is near a fixed color.</summary>
    private const int NearColor = 24;

    /// <summary>The navy of Prevue, as the background of the channel logos.</summary>
    public static readonly AmigaColor Navy = new(0, 0, 4);

    /// <summary>Makes the ILBM file of a picture.</summary>
    public static byte[] Create(RgbaImage image, PictureLayout layout, PictureFit fit, AmigaColor background,
        bool keyBackground)
    {
        var (palette, pixels) = Render(image, layout, fit, background, keyBackground);
        return IlbmImage.Write(layout.Width, layout.Height, layout.Planes, palette, pixels, layout.Format);
    }

    /// <summary>The palette and the palette index of each pixel of a picture.</summary>
    public static (List<AmigaColor> Palette, byte[] Pixels) Render(RgbaImage image, PictureLayout layout,
        PictureFit fit, AmigaColor background, bool keyBackground)
    {
        int width = layout.Width, height = layout.Height;
        var visible = Math.Min(height, layout.VisibleHeight ?? height);

        // The size of the image in pixels of the picture, with its shape on the screen.
        int imageWidth, imageHeight;
        if (fit == PictureFit.Stretch)
            (imageWidth, imageHeight) = (width, visible);
        else
        {
            var boxWidth = width * layout.PixelAspect;
            var scaleX = boxWidth / image.Width;
            var scaleY = (double)visible / image.Height;
            var scale = fit == PictureFit.Cover ? Math.Max(scaleX, scaleY) : Math.Min(scaleX, scaleY);
            imageWidth = Math.Max(1, (int)Math.Round(image.Width * scale / layout.PixelAspect));
            imageHeight = Math.Max(1, (int)Math.Round(image.Height * scale));
        }

        var left = (width - imageWidth) / 2;
        var top = (visible - imageHeight) / 2;
        var scaled = Quantizer.Scale(image, imageWidth, imageHeight);

        // The picture in 8-bit RGB, on the background. A background pixel is not a pixel of the image, or a pixel of
        // the image that is more than half transparent.
        var rgb = new int[width * height * 3];
        var back = new bool[width * height];
        var (backR, backG, backB) = background.Rgb;
        for (var i = 0; i < width * height; i++)
        {
            (rgb[i * 3], rgb[i * 3 + 1], rgb[i * 3 + 2]) = (backR, backG, backB);
            back[i] = true;
        }

        for (var y = Math.Max(0, top); y < Math.Min(visible, top + imageHeight); y++)
        {
            for (var x = Math.Max(0, left); x < Math.Min(width, left + imageWidth); x++)
            {
                var source = ((y - top) * imageWidth + x - left) * 4;
                var alpha = scaled[source + 3];
                var target = y * width + x;
                if (alpha < 128)
                    continue;
                back[target] = false;
                var part = alpha / 255.0;
                for (var c = 0; c < 3; c++)
                    rgb[target * 3 + c] = (int)Math.Round(scaled[source + c] * part + rgb[target * 3 + c] * (1 - part));
            }
        }

        // The background pixels get one color without a dither: color 0 (the genlock key), or the background color
        // of the banner (color 7). A logo without the key has its background in the colors of the dither.
        var hasBackgroundIndex = layout.BackgroundIndex > 0;
        var fixedBackground = keyBackground || hasBackgroundIndex;
        var keyIndex = keyBackground ? 0 : layout.BackgroundIndex;

        // The free colors come from the pixels of the dither. A pixel near a fixed color (a color of the grid, or the
        // background of the banner) does not need a free color.
        var fixedValues = (layout.FixedColors ?? []).Append(background).Select(c => c.Rgb).ToArray();
        var opaque = new List<int>();
        for (var i = 0; i < width * height; i++)
        {
            if (fixedBackground && back[i])
                continue;
            var (r, g, b) = (rgb[i * 3], rgb[i * 3 + 1], rgb[i * 3 + 2]);
            if (hasBackgroundIndex && fixedValues.Any(f =>
                    Math.Abs(f.R - r) <= NearColor && Math.Abs(f.G - g) <= NearColor && Math.Abs(f.B - b) <= NearColor))
                continue;
            opaque.AddRange([r, g, b]);
        }

        // A logo: color 0 is the background. The banner: colors 0 to 3 are the colors of the grid, and color 7 is
        // the background.
        var palette = layout.FixedColors?.ToList() ?? [background];
        var free = layout.Colors - palette.Count - (hasBackgroundIndex ? 1 : 0);
        if (opaque.Count > 0)
            palette.AddRange(Quantizer.MedianCut([.. opaque], free).Take(free));
        // The dither needs a color other than color 0.
        if (palette.Count == 1)
            palette.Add(background);
        while (palette.Count < layout.Colors)
            palette.Add(background);
        if (hasBackgroundIndex)
            palette[layout.BackgroundIndex] = background;

        Quantizer.Dither(rgb, width, height, palette, fixedBackground ? back : null, keyIndex);
        var pixels = new byte[width * height];
        for (var i = 0; i < width * height; i++)
            pixels[i] = (byte)rgb[i * 3];
        return (palette, pixels);
    }
}
