using System.Text;
using Prevue.Graphics;
using PrevueCommander.Model.PlaybookCommands;

namespace PrevueCommander;

/// <summary>Converts the PNG files of the playbook to IFF pictures for ESQ.</summary>
public static class Pictures
{
    /// <summary>Reads a PNG file and makes its IFF picture. Optionally writes a copy to a local directory.</summary>
    public static async Task<byte[]> ConvertAsync(PlaybookContext context, string source, PictureLayout layout,
        PictureFit fit, string background, bool keyBackground, string name, string? saveTo,
        CancellationToken cancellationToken)
    {
        var path = context.ResolvePath(source);
        RgbaImage image;
        try
        {
            image = PngImage.Read(await File.ReadAllBytesAsync(path, cancellationToken));
        }
        catch (InvalidDataException e)
        {
            throw new FormatException($"{path}: {e.Message}", e);
        }

        var picture = AdPicture.Create(image, layout, fit, AmigaColor.Parse(background), keyBackground);
        if (saveTo != null)
        {
            var directory = context.ResolvePath(saveTo);
            Directory.CreateDirectory(directory);
            await File.WriteAllBytesAsync(Path.Combine(directory, name), picture, cancellationToken);
        }

        return picture;
    }

    public static byte[] Latin1(string text) => Encoding.Latin1.GetBytes(text);
}
