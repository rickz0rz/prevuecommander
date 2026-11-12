using Prevue.Commands;
using Prevue.Graphics;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>
/// Converts a PNG file to the banner between the rows of the grid, and sends it to <c>DF0:Banner/ADBANNER.UV</c> with
/// a new <c>BANNER.INI</c>. Then it tells ESQ to load the banner.
/// </summary>
/// <remarks>
/// The banner has 640 pixels across and 3 bit planes. ESQ takes only colors 4 to 7 from it, because colors 0 to 3 are
/// the colors of the grid. Color 7 is the background, and ESQ fills the row around the banner with it.
/// </remarks>
public record BannerPlaybookCommand : IPlaybookCommand
{
    public const string File = "Banner/ADBANNER.UV";

    /// <summary>The PNG file. A relative path starts at the directory of the playbook.</summary>
    public string Source { get; init; } = string.Empty;

    public PictureFit Fit { get; init; } = PictureFit.Contain;

    /// <summary>The color around the image. The default is the navy of the grid.</summary>
    public string Background { get; init; } = "#000033";

    /// <summary>The rows of the banner, from 2 to 240.</summary>
    public int Height { get; init; } = 160;

    /// <summary>True sends the utility command that loads the banner.</summary>
    public bool Reload { get; init; } = true;

    /// <summary>A local directory that gets a copy of the IFF file.</summary>
    public string? SaveTo { get; init; }

    public string Address { get; init; } = "*";

    public async Task<IReadOnlyList<BaseCommand>> TransformAsync(PlaybookContext context,
        CancellationToken cancellationToken)
    {
        if (Height is < 2 or > PictureLayout.MaximumBannerHeight)
            throw new ArgumentException($"The height of the banner must be from 2 to {PictureLayout.MaximumBannerHeight}.");

        var layout = PictureLayout.Banner with { Height = Height };
        var picture = await Pictures.ConvertAsync(context, Source, layout, Fit, Background, keyBackground: false,
            "ADBANNER.UV", SaveTo, cancellationToken);
        context.Log.WriteLine($"Banner {Source}: {File} ({picture.Length} bytes).");

        var ini = Pictures.Latin1($"[BANNER]\r\n\r\nFILENAME=df0:{File}\r\n");
        var commands = new List<BaseCommand>();
        commands.AddRange(FileTransferPlaybookCommand.Commands($"DF0:{File}", picture, Address));
        commands.AddRange(FileTransferPlaybookCommand.Commands("DF0:BANNER.INI", ini, Address));
        if (Reload)
            commands.Add(new UtilityCommand(UtilityAction.ReloadBanner));
        return commands;
    }
}
