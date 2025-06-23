using System.Text.Json;

namespace PrevueCommander.ChannelsDVR;

public class ChannelsAPI
{
    private readonly string _address;

    public ChannelsAPI(string address)
    {
        _address = address;
    }

    public async Task<JsonDocument> GetChannels()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{_address}/api/v1/channels");
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }

    public async Task<JsonDocument> GetGuide()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{_address}/devices/ANY/guide");
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }
}