using PrevueCommander.Model.PlaybookCommands;

namespace PrevueCommander.Server;

/// <summary>
/// The body of <c>POST /prevue/ctrl/promo</c>, as Esquire reads it. <see cref="Title"/>, <see cref="Channels"/> and
/// <see cref="Brush"/> at the top are for the right box.
/// </summary>
public sealed record PromoRequest
{
    public PromoBoxSettings? Right { get; init; }
    public PromoBoxSettings? Left { get; init; }
    public PromoSide First { get; init; } = PromoSide.Right;
    public Prevue.Commands.Control.PromoStyle Style { get; init; } = Prevue.Commands.Control.PromoStyle.Box;
    public string? Title { get; init; }
    public string? Channels { get; init; }
    public string? Brush { get; init; }

    public PromoPlaybookCommand ToCommand()
    {
        var right = Right ?? (Title == null ? null : new PromoBoxSettings
        {
            Title = Title, Channels = Channels ?? "*", Brush = Brush
        });
        if (string.IsNullOrWhiteSpace(right?.Title) && string.IsNullOrWhiteSpace(Left?.Title))
            throw new ArgumentException("A promo needs a box with a title: \"right\", \"left\" or \"title\".");
        return new PromoPlaybookCommand { Right = right, Left = Left, First = First, Style = Style };
    }
}

/// <summary>One packet of <c>POST /prevue/ctrl/packets</c>.</summary>
public sealed record PacketRequest(int Type, string? Body);

/// <summary>The body of <c>POST /prevue/ctrl/weather</c>.</summary>
/// <param name="Forecast">True shows the forecast (<c>W3</c>), false the current weather (<c>W0</c>).</param>
public sealed record ShowWeatherRequest(bool Forecast = false);

/// <summary>The body of <c>POST /prevue/ctrl/hold</c>.</summary>
/// <param name="Minutes">The time of the hold. Null holds until <c>DELETE /prevue/ctrl/hold</c>.</param>
public sealed record HoldRequest(double? Minutes = null, int IntervalSeconds = TopHalfHold.DefaultIntervalSeconds);

/// <summary>The body of <c>POST /prevue/data/local-ads</c>.</summary>
/// <param name="DryRun">True returns the preview and sends nothing.</param>
public sealed record LocalAdsRequest(List<string>? Ads = null, bool DryRun = false);

/// <summary>The body of <c>POST /prevue/data/weather</c>.</summary>
/// <param name="Forecast">The 3-day forecast. With a forecast, <paramref name="Show"/> shows the forecast (W3).</param>
/// <param name="Show">True shows the weather after the session.</param>
/// <param name="PauseSeconds">The pause between the session and the show command.</param>
public sealed record WeatherRequest(
    WeatherStatusPlaybookCommand? Status = null,
    WeatherTextPlaybookCommand? Text = null,
    WeatherForecastPlaybookCommand? Forecast = null,
    bool Show = true,
    double PauseSeconds = 5);

/// <summary>
/// The fields of <c>POST /prevue/data/weather-gov</c> that are not settings of <c>!!weatherGov</c>. The body also has
/// the settings, for example <c>"zip"</c>.
/// </summary>
/// <param name="Show">"none", "current" (W0) or "forecast" (W3).</param>
/// <param name="PauseSeconds">The pause between the session and the show command.</param>
public sealed record WeatherGovRequest(string? Show = null, double PauseSeconds = 5);

/// <summary>The body of <c>POST /prevue/data/clock</c>.</summary>
/// <param name="Date">A local date and time, for example "2020-11-01T16:00". Null sends the current time.</param>
public sealed record ClockRequest(string? Date = null);

/// <summary>The answer to a request that queued a job.</summary>
public sealed record QueuedAnswer(JobInfo Job, object? Details = null);

/// <summary>The answer to a request with an error.</summary>
public sealed record ErrorAnswer(string Error);
