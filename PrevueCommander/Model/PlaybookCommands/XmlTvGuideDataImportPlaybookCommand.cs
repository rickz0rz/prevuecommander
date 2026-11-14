using Prevue.Commands.Listings;
using PrevueCommander.Model.PlaybookCommands.CommandObjects;
using PrevueCommander.XmlTv;
using PrevueCommander.XmlTv.Model;

namespace PrevueCommander.Model.PlaybookCommands;

/// <summary>Imports the guide of one or more XMLTV files.</summary>
public record XmlTvGuideDataImportPlaybookCommand : GuideDataImportPlaybookCommand
{
    public List<XmlTvFile> XmlTvFiles { get; init; } = [];

    protected override async Task<List<GuideChannel>> LoadChannelsAsync(PlaybookContext context,
        CancellationToken cancellationToken)
    {
        var channels = new List<GuideChannel>();
        HttpClient? http = null;
        try
        {
            foreach (var file in XmlTvFiles)
            {
                Tv tv;
                if (WebAddress(file.Path) is { } address)
                {
                    context.Log.WriteLine($"Downloading the XMLTV file {address}.");
                    http ??= new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                    tv = await XmlTvGuide.DownloadAsync(http, address, cancellationToken);
                }
                else
                {
                    var path = context.ResolvePath(file.Path);
                    if (!File.Exists(path))
                        throw new FileNotFoundException($"The XMLTV file {path} does not exist.", path);
                    tv = await XmlTvGuide.ParseFileAsync(path, cancellationToken);
                }

                channels.AddRange(Limit(XmlTvGuide.Convert(tv), file.MaximumNumberOfChannels));
            }
        }
        finally
        {
            http?.Dispose();
        }

        return channels;
    }

    /// <summary>The address, if the path is an HTTP or HTTPS address.</summary>
    public static Uri? WebAddress(string path) =>
        Uri.TryCreate(path, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri
            : null;
}
