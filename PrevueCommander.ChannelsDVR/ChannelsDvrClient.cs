using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrevueCommander.ChannelsDVR;

/// <summary>A channel of the guide of a Channels DVR server.</summary>
public sealed record ChannelsDvrChannel(
    [property: JsonPropertyName("Number")] string Number,
    [property: JsonPropertyName("Name")] string? Name,
    [property: JsonPropertyName("CallSign")] string? CallSign,
    [property: JsonPropertyName("Station")] string? Station,
    [property: JsonPropertyName("HD")] bool HD,
    [property: JsonPropertyName("Hidden")] bool Hidden,
    [property: JsonPropertyName("Image")] string? Image = null);

/// <summary>
/// A program on a channel. <see cref="Time"/> is a Unix time, and the duration is in seconds. A sports event has an
/// event title, for example "Detroit Lions at Carolina Panthers".
/// </summary>
public sealed record ChannelsDvrAiring(
    [property: JsonPropertyName("Time")] long Time,
    [property: JsonPropertyName("Duration")] long Duration,
    [property: JsonPropertyName("Title")] string? Title,
    [property: JsonPropertyName("Categories")] string[]? Categories,
    [property: JsonPropertyName("Tags")] string[]? Tags,
    [property: JsonPropertyName("Summary")] string? Summary,
    [property: JsonPropertyName("ContentRating")] string? ContentRating,
    [property: JsonPropertyName("ReleaseYear")] int? ReleaseYear,
    [property: JsonPropertyName("EventTitle")] string? EventTitle);

public sealed record ChannelsDvrGuideEntry(
    [property: JsonPropertyName("Channel")] ChannelsDvrChannel Channel,
    [property: JsonPropertyName("Airings")] ChannelsDvrAiring[]? Airings);

/// <summary>Reads the guide of a Channels DVR server with its HTTP API.</summary>
public sealed class ChannelsDvrClient : IDisposable
{
    private readonly HttpClient _http;

    public ChannelsDvrClient(string serverAddress)
    {
        if (!Uri.TryCreate(serverAddress, UriKind.Absolute, out var server))
            throw new ArgumentException($"The Channels DVR address \"{serverAddress}\" is not valid.",
                nameof(serverAddress));
        _http = new HttpClient { BaseAddress = server, Timeout = TimeSpan.FromMinutes(2) };
    }

    /// <summary>GET /devices/ANY/guide: the channels and their programs from a time, for a duration.</summary>
    public async Task<ChannelsDvrGuideEntry[]> GetGuideAsync(DateTimeOffset start, TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var path = $"devices/ANY/guide?time={start.ToUnixTimeSeconds()}&duration={(long)duration.TotalSeconds}";
        using var response = await _http.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<ChannelsDvrGuideEntry[]>(stream,
            cancellationToken: cancellationToken) ?? [];
    }

    public void Dispose() => _http.Dispose();
}
