using System.Security.Cryptography;
using System.Text;
using Prevue.Commands;
using Prevue.Commands.Listings;
using Prevue.Graphics;
using PrevueCommander.Model.PlaybookCommands.CommandObjects;
using PrevueCommander.Model.PlaybookCommands;

namespace PrevueCommander;

/// <summary>Makes the commands that send the channel logos of an import and the new LOGO.LST.</summary>
public static class ChannelLogoBuilder
{
    public const string Folder = "Logos/Channels";

    /// <summary>
    /// Downloads and converts the logo image of each channel, and makes the file transfers. ESQ matches a channel
    /// logo with a channel by its file name, so the file name is the source name of the channel in the lineup.
    /// </summary>
    /// <param name="channels">The channels of the lineup, in the order of the lineup.</param>
    public static async Task<List<BaseCommand>> BuildAsync(IReadOnlyList<GuideChannel> channels,
        ChannelLogoSettings settings, PlaybookContext context, CancellationToken cancellationToken)
    {
        var lineUp = channels.Take(ChannelLineUpCommand.MaximumChannels).ToList();
        var sourceNames = ListingsBuilder.SourceNames(lineUp.Select(c => c.CallSign));
        var cache = settings.CacheDirectory == null ? null : context.ResolvePath(settings.CacheDirectory);
        var saveTo = settings.SaveTo == null ? null : context.ResolvePath(settings.SaveTo);
        if (cache != null)
            Directory.CreateDirectory(cache);
        if (saveTo != null)
            Directory.CreateDirectory(saveTo);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        var commands = new List<BaseCommand>();
        var names = new List<string>();
        var bytes = 0;
        for (var i = 0; i < lineUp.Count; i++)
        {
            if (lineUp[i].LogoUrl is not { Length: > 0 } url)
                continue;

            byte[] picture;
            try
            {
                var png = await DownloadAsync(http, url, cache, cancellationToken);
                picture = ChannelLogo.Create(PngImage.Read(png));
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidDataException
                                          or IOException && !cancellationToken.IsCancellationRequested)
            {
                context.Log.WriteLine($"The logo of {lineUp[i].CallSign} is left out: {e.Message}");
                continue;
            }

            if (saveTo != null)
                await File.WriteAllBytesAsync(Path.Combine(saveTo, sourceNames[i]), picture, cancellationToken);
            commands.AddRange(FileTransferPlaybookCommand.Commands($"DF0:{Folder}/{sourceNames[i]}", picture));
            names.Add(sourceNames[i]);
            bytes += picture.Length;
        }

        context.Log.WriteLine($"Channel logos: {names.Count} pictures, {bytes} bytes.");
        commands.AddRange(await new LogoListPlaybookCommand
        {
            Lines = [.. settings.KeepLines, .. names.Select(name => $"{Folder}/{name}")],
            Reload = settings.Reload
        }.TransformAsync(context, cancellationToken));
        return commands;
    }

    private static async Task<byte[]> DownloadAsync(HttpClient http, string url, string? cache,
        CancellationToken cancellationToken)
    {
        if (cache == null)
            return await http.GetByteArrayAsync(url, cancellationToken);

        var file = Path.Combine(cache,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..16] + ".png");
        if (!File.Exists(file))
            await File.WriteAllBytesAsync(file, await http.GetByteArrayAsync(url, cancellationToken),
                cancellationToken);
        return await File.ReadAllBytesAsync(file, cancellationToken);
    }
}
