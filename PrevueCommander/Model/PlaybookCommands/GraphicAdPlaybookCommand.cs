using Prevue.Commands;
using Prevue.Graphics;

namespace PrevueCommander.Model.PlaybookCommands;

public enum AdResolution
{
    /// <summary>320 by 240 pixels with 32 colors, as the logos of the drive. For photos and color gradients.</summary>
    Low,

    /// <summary>640 by 240 pixels with 16 colors. For small text and thin lines.</summary>
    High
}

/// <summary>
/// Converts a PNG file to an IFF picture and sends it to <c>DF0:Logos/Ads/NAME</c>. ESQ shows the picture in the top
/// half of the screen when <c>LOGO.LST</c> has the line <c>Logos/Ads/NAME,</c> (see
/// <see cref="LogoListPlaybookCommand"/>).
/// </summary>
public record GraphicAdPlaybookCommand : IPlaybookCommand
{
    public const string Folder = "Logos/Ads";

    /// <summary>The PNG file. A relative path starts at the directory of the playbook.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// The name of the picture on the drive: 1 to 8 letters or digits. The default is the first 8 letters and digits
    /// of the file name, in upper case.
    /// </summary>
    public string? Name { get; init; }

    public AdResolution Resolution { get; init; } = AdResolution.Low;

    public PictureFit Fit { get; init; } = PictureFit.Contain;

    /// <summary>The color around the image and behind its transparent pixels.</summary>
    public string Background { get; init; } = "#000044";

    /// <summary>True makes the background the genlock key, so that the genlock video shows through it.</summary>
    public bool KeyBackground { get; init; } = true;

    /// <summary>A local directory that gets a copy of the IFF file, to look at it before ESQ shows it.</summary>
    public string? SaveTo { get; init; }

    public string Address { get; init; } = "*";

    /// <summary>The line of the picture for LOGO.LST.</summary>
    public string LogoListLine => $"{Folder}/{PictureName()},";

    public async Task<IReadOnlyList<BaseCommand>> TransformAsync(PlaybookContext context,
        CancellationToken cancellationToken)
    {
        var name = PictureName();
        var layout = Resolution == AdResolution.High ? PictureLayout.HighResolution : PictureLayout.LowResolution;
        var picture = await Pictures.ConvertAsync(context, Source, layout, Fit, Background, KeyBackground, name, SaveTo,
            cancellationToken);
        context.Log.WriteLine($"Graphic ad {Source}: {Folder}/{name} ({picture.Length} bytes). LOGO.LST needs the " +
                              $"line {LogoListLine}");
        return FileTransferPlaybookCommand.Commands($"DF0:{Folder}/{name}", picture, Address);
    }

    private string PictureName()
    {
        var name = Name ?? new string(Path.GetFileNameWithoutExtension(Source).Where(char.IsAsciiLetterOrDigit)
            .Take(8).ToArray()).ToUpperInvariant();
        if (name.Length is 0 or > 8 || !name.All(char.IsAsciiLetterOrDigit))
            throw new ArgumentException($"The name of a graphic ad must have 1 to 8 letters or digits: \"{name}\".");
        return name;
    }
}
