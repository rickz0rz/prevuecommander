namespace PrevueCommander.WeatherGov;

/// <summary>
/// Chooses the weather picture of ESQ for a weather.gov condition. ESQ has five pictures on the drive: PWI1 the sun
/// (icon 2), PWI2 clouds (3), PWI3 rain (4), PWI4 the sun with clouds (5) and PWI5 snow (6).
/// </summary>
public static class WeatherIcons
{
    public const int Sun = 2;
    public const int Clouds = 3;
    public const int Rain = 4;
    public const int PartlySunny = 5;
    public const int Snow = 6;

    private static readonly Dictionary<string, int> Codes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["skc"] = Sun, ["few"] = Sun, ["wind_skc"] = Sun, ["wind_few"] = Sun, ["hot"] = Sun, ["cold"] = Sun,
        ["sct"] = PartlySunny, ["bkn"] = PartlySunny, ["wind_sct"] = PartlySunny, ["wind_bkn"] = PartlySunny,
        ["ovc"] = Clouds, ["wind_ovc"] = Clouds, ["fog"] = Clouds, ["haze"] = Clouds, ["smoke"] = Clouds,
        ["dust"] = Clouds,
        ["rain"] = Rain, ["rain_showers"] = Rain, ["rain_showers_hi"] = Rain, ["tsra"] = Rain, ["tsra_sct"] = Rain,
        ["tsra_hi"] = Rain, ["fzra"] = Rain, ["rain_fzra"] = Rain, ["rain_sleet"] = Rain, ["sleet"] = Rain,
        ["tornado"] = Rain, ["hurricane"] = Rain, ["tropical_storm"] = Rain,
        ["snow"] = Snow, ["rain_snow"] = Snow, ["snow_sleet"] = Snow, ["snow_fzra"] = Snow, ["blizzard"] = Snow
    };

    /// <summary>
    /// The picture for the icon link of weather.gov, for example ".../icons/land/day/rain,40/bkn?size=medium". The
    /// first condition of the link is the main condition. If the link has no known code, the text decides.
    /// </summary>
    /// <returns>2 to 6, or null if neither the link nor the text has a known condition.</returns>
    public static int? Choose(string? link, string? text)
    {
        if (link != null && Uri.TryCreate(link, UriKind.Absolute, out var uri))
        {
            // The path is /icons/land/{day|night}/{code[,percent]}[/{code[,percent]}].
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var time = Array.FindIndex(segments, s => s is "day" or "night");
            if (time >= 0 && time + 1 < segments.Length &&
                Codes.TryGetValue(segments[time + 1].Split(',')[0], out var icon))
                return icon;
        }

        return FromText(text);
    }

    private static int? FromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var t = text.ToLowerInvariant();
        if (t.Contains("snow") || t.Contains("flurr") || t.Contains("blizzard"))
            return Snow;
        if (t.Contains("rain") || t.Contains("shower") || t.Contains("storm") || t.Contains("drizzle") ||
            t.Contains("sleet"))
            return Rain;
        if (t.Contains("partly") || t.Contains("mostly sunny") || t.Contains("mostly clear"))
            return PartlySunny;
        if (t.Contains("sunny") || t.Contains("clear") || t.Contains("fair"))
            return Sun;
        if (t.Contains("cloud") || t.Contains("overcast") || t.Contains("fog") || t.Contains("haze"))
            return Clouds;
        return null;
    }
}
