using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Prevue.Commands;
using Prevue.Commands.Control;
using PrevueCommander.Model.PlaybookCommands;
using YamlDotNet.Core;

namespace PrevueCommander.Server;

/// <summary>The parts of the program that the web server uses.</summary>
/// <param name="Context">Makes the context of a request: the time, the directory of the playbook and the log.</param>
/// <param name="Session">The session commands of the playbook, for the data line requests.</param>
/// <param name="Check">Throws if a command needs a line that the configuration does not have.</param>
/// <param name="LineStatus">Tells if a line is connected, and the bytes that it sent.</param>
/// <param name="Watcher">The watch mode, or null if the playbook has no guide import.</param>
/// <param name="WatchMinutes">The interval of the watch mode, or 0.</param>
/// <param name="Promoter">The automatic promos, or null.</param>
public sealed record ServerParts(
    Func<PlaybookContext> Context,
    SessionTemplate Session,
    Action<IEnumerable<BaseCommand>> Check,
    Func<SerialLine, (bool Connected, long BytesSent)> LineStatus,
    double ControlBaudRate,
    GuideWatcher? Watcher = null,
    int WatchMinutes = 0,
    AutoPromoter? Promoter = null);

/// <summary>
/// The web server: the control page and the HTTP API. The paths and the bodies that Esquire also has
/// (<c>/prevue/ctrl/promo</c>, <c>/clear</c>, <c>/logo</c>, <c>/packets</c> and <c>GET /prevue/ctrl</c>) are the same, so
/// a script works with both.
/// </summary>
/// <remarks>
/// A request that sends commands makes all its commands first. A request with an error gets status 400, and nothing is
/// sent. Otherwise the request adds a job to the <see cref="JobQueue"/> and gets status 202 at once.
/// </remarks>
public sealed class ControlServer(ServerParts parts, JobQueue jobs, TopHalfHold hold, Func<DateTime> clock,
    TextWriter log)
{
    private const int MaximumBodyBytes = 1024 * 1024;
    private const string PageResource = "PrevueCommander.index.html";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        // The answers go to scripts and to the page, not into HTML, so quotes and accents stay readable.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    /// <summary>
    /// The URL prefixes of a host and a port. 0.0.0.0 accepts all the network interfaces. HttpListener compares the
    /// host name of each request, so the local computer gets both of its names.
    /// </summary>
    public static IReadOnlyList<string> Prefixes(string host, int port) => host switch
    {
        "0.0.0.0" or "*" or "+" => [$"http://+:{port}/"],
        "127.0.0.1" or "localhost" => [$"http://127.0.0.1:{port}/", $"http://localhost:{port}/"],
        _ => [$"http://{host}:{port}/"]
    };

    /// <summary>Accepts requests until the cancellation.</summary>
    public async Task RunAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var listener = new HttpListener();
        foreach (var prefix in Prefixes(host, port))
            listener.Prefixes.Add(prefix);
        try
        {
            listener.Start();
        }
        catch (HttpListenerException e)
        {
            throw new InvalidOperationException($"The web server cannot start on {host}:{port}: {e.Message}", e);
        }

        log.WriteLine($"Web server: http://{(host is "0.0.0.0" or "*" or "+" ? "localhost" : host)}:{port}/ " +
                      "Press Ctrl+C to stop.");
        await using var stop = cancellationToken.Register(listener.Stop);
        var requests = new List<Task>();
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch (Exception e) when (e is HttpListenerException or ObjectDisposedException
                                          or InvalidOperationException && cancellationToken.IsCancellationRequested)
            {
                break;
            }

            requests.RemoveAll(t => t.IsCompleted);
            requests.Add(Task.Run(() => ServeAsync(context, cancellationToken), CancellationToken.None));
        }

        await Task.WhenAll(requests);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task ServeAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;
        var method = request.HttpMethod;
        var path = request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;
        try
        {
            if (method == "GET" && path is "" or "/index.html")
            {
                await WriteAsync(response, 200, Page(), "text/html; charset=utf-8");
                return;
            }

            int status;
            object answer;
            try
            {
                var body = await ReadBodyAsync(request, cancellationToken);
                (status, answer) = await HandleAsync(method, path, body, cancellationToken);
            }
            catch (InvalidDataException e)
            {
                (status, answer) = (413, new ErrorAnswer(e.Message));
            }

            await WriteAsync(response, status, JsonSerializer.Serialize(answer, Json), "application/json");
            if (method != "GET")
                log.WriteLine($"{clock():HH:mm:ss} HTTP {method} {path}: {status}");
        }
        catch (Exception e) when (e is HttpListenerException or IOException or ObjectDisposedException)
        {
            // The client closed the connection.
        }
        finally
        {
            response.Close();
        }
    }

    /// <summary>Handles one request.</summary>
    /// <returns>The HTTP status and the object for the JSON answer.</returns>
    public async Task<(int Status, object Answer)> HandleAsync(string method, string path, string body,
        CancellationToken cancellationToken)
    {
        try
        {
            return (method, path) switch
            {
                ("GET", "/status") => (200, Status()),
                ("GET", "/prevue/ctrl") => (200, ControlLine()),
                ("GET", "/jobs") => (200, jobs.Jobs()),
                ("GET", _) when JobId(path) is { } id => jobs.Find(id) is { } job
                    ? (200, job)
                    : (404, new ErrorAnswer($"No job has the id {id}.")),
                ("DELETE", _) when JobId(path) is { } id => jobs.Cancel(id)
                    ? (200, jobs.Find(id)!)
                    : (409, new ErrorAnswer($"The job {id} does not exist or has started. Only a queued job can be cancelled.")),
                ("POST", "/prevue/ctrl/promo") => Queue("Promo",
                    await TransformAsync(Read<PromoRequest>(body).ToCommand(), cancellationToken)),
                ("POST", "/prevue/ctrl/clear") => Queue("Clear the top half", [new ClearTopHalfCommand()]),
                ("POST", "/prevue/ctrl/logo") => Queue("Show the logo", [new ShowLogoCommand()]),
                ("POST", "/prevue/ctrl/ad") => Queue("Ad break", [new RawControlCommand(12, string.Empty)]),
                ("POST", "/prevue/ctrl/weather") => Queue("Show the weather",
                    [Read<ShowWeatherRequest>(body, new ShowWeatherRequest()).Forecast
                        ? new RawControlCommand(1, "W3")
                        : new ShowWeatherCommand()]),
                ("POST", "/prevue/ctrl/packets") => Queue("Packets",
                    (Read<List<PacketRequest>>(body) ?? []).Select(BaseCommand (p) => new RawControlCommand(p.Type, p.Body ?? string.Empty)).ToList()),
                ("POST", "/prevue/ctrl/hold") => StartHold(Read<HoldRequest>(body, new HoldRequest())),
                ("DELETE", "/prevue/ctrl/hold") => hold.Stop()
                    ? (200, hold.Info)
                    : (409, new ErrorAnswer("No hold is active.")),
                ("POST", "/prevue/data/local-ads") => await LocalAdsAsync(Read<LocalAdsRequest>(body), cancellationToken),
                ("POST", "/prevue/data/weather") => await WeatherAsync(Read<WeatherRequest>(body), cancellationToken),
                ("POST", "/prevue/data/clock") => Queue("Clock", parts.Session.Wrap(await TransformAsync(
                    new ClockPlaybookCommand { Date = Read<ClockRequest>(body, new ClockRequest()).Date },
                    cancellationToken))),
                ("POST", "/prevue/guide/refresh") => await RefreshAsync(cancellationToken),
                ("POST", "/prevue/commands") => await CommandsAsync(body, cancellationToken),
                _ => (404, new ErrorAnswer($"The server has no {method} {path}. GET /status shows the state."))
            };
        }
        catch (Exception e) when (e is JsonException or ArgumentException or FormatException
                                      or InvalidOperationException or YamlException or InvalidDataException
                                      or IOException or HttpRequestException)
        {
            var message = e is YamlException { InnerException: { } inner } ? $"{e.Message} {inner.Message}" : e.Message;
            return (400, new ErrorAnswer(message));
        }
    }

    private (int, object) Queue(string name, IReadOnlyList<BaseCommand> commands, object? details = null)
    {
        parts.Check(commands);
        return (202, new QueuedAnswer(jobs.Enqueue(name, commands).Info(), details));
    }

    private (int, object) StartHold(HoldRequest request)
    {
        parts.Check([new ClearTopHalfCommand()]);
        return (202, hold.Start(request.Minutes, request.IntervalSeconds));
    }

    private async Task<(int, object)> LocalAdsAsync(LocalAdsRequest request, CancellationToken cancellationToken)
    {
        var command = new LocalAdsPlaybookCommand { Ads = request.Ads ?? [] };
        var context = parts.Context() with { Log = TextWriter.Null };
        var commands = await command.TransformAsync(context, cancellationToken);
        var previews = commands.OfType<LocalAdCommand>().Select(LocalAdsPlaybookCommand.Preview).ToList();
        return request.DryRun
            ? (200, new { ads = previews })
            : Queue(previews.Count == 1 ? "1 local ad" : $"{previews.Count} local ads", parts.Session.Wrap(commands),
                new { ads = previews });
    }

    private async Task<(int, object)> WeatherAsync(WeatherRequest request, CancellationToken cancellationToken)
    {
        if (request.Status == null && request.Text == null)
            throw new ArgumentException("The weather needs \"status\", \"text\" or both.");

        var data = new List<BaseCommand>();
        if (request.Status != null)
            data.AddRange(await TransformAsync(request.Status, cancellationToken));
        if (request.Text != null)
            data.AddRange(await TransformAsync(request.Text, cancellationToken));

        var commands = parts.Session.Wrap(data);
        if (request.Show)
        {
            commands.Add(new PauseCommand(TimeSpan.FromSeconds(Math.Max(0, request.PauseSeconds))));
            commands.Add(new ShowWeatherCommand());
        }

        return Queue("Weather", commands);
    }

    private async Task<(int, object)> RefreshAsync(CancellationToken cancellationToken)
    {
        if (parts.Watcher == null)
            return (409, new ErrorAnswer("The playbook has no guide data import, so the server cannot read the guide."));

        var changes = await parts.Watcher.UpdateAsync(cancellationToken);
        return (200, new { changes, error = parts.Watcher.LastError });
    }

    /// <summary>
    /// Runs playbook commands from a YAML body: a playbook with <c>commands:</c>, or only the list of commands. The
    /// server sends the commands as they are written, without an added session.
    /// </summary>
    private async Task<(int, object)> CommandsAsync(string body, CancellationToken cancellationToken)
    {
        // A bare list of commands becomes the commands of a playbook.
        if (body.TrimStart().StartsWith('-'))
            body = "commands:\n" + string.Join('\n', body.Split('\n').Select(line => "  " + line.TrimEnd('\r')));
        var playbook = PlaybookLoader.Parse(body);
        if (playbook.Commands.Count == 0)
            throw new ArgumentException("The body has no playbook commands. Send YAML, for example \"- !!clearTopHalf {}\".");

        var context = parts.Context();
        var commands = new List<BaseCommand>();
        foreach (var command in playbook.Commands)
            commands.AddRange(await command.TransformAsync(context, cancellationToken));
        return Queue(playbook.Commands.Count == 1 ? "1 playbook command" : $"{playbook.Commands.Count} playbook commands",
            commands);
    }

    private Task<IReadOnlyList<BaseCommand>> TransformAsync(IPlaybookCommand command,
        CancellationToken cancellationToken) => command.TransformAsync(parts.Context(), cancellationToken);

    private object Status()
    {
        var (control, data) = jobs.Waiting();
        object Line(SerialLine line)
        {
            var (connected, sent) = parts.LineStatus(line);
            return new { connected, bytesSent = sent };
        }

        var watcher = parts.Watcher;
        var promoter = parts.Promoter;
        return new
        {
            time = clock(),
            lines = new { data = Line(SerialLine.Data), control = Line(SerialLine.Control) },
            queues = new { control, data },
            hold = hold.Info,
            watch = new
            {
                enabled = parts.WatchMinutes > 0,
                minutes = parts.WatchMinutes,
                canRefresh = watcher != null,
                lastRead = watcher?.LastRead,
                lastChanges = watcher?.LastChanges,
                lastError = watcher?.LastError
            },
            autoPromo = new
            {
                enabled = promoter != null,
                lastTime = promoter?.LastTime,
                lastTitle = promoter?.LastProgram?.Title
            },
            jobs = jobs.Jobs().Take(10)
        };
    }

    /// <summary>The state of the control line, with the fields of <c>GET /prevue/ctrl</c> of Esquire.</summary>
    private object ControlLine()
    {
        var queued = jobs.ControlBytesWaiting();
        var seconds = parts.ControlBaudRate > 0 ? Math.Round(queued * 10 / parts.ControlBaudRate, 1) : 0;
        return new { queued, seconds, sent = parts.LineStatus(SerialLine.Control).BytesSent };
    }

    private static int? JobId(string path) =>
        path.StartsWith("/jobs/", StringComparison.Ordinal) && int.TryParse(path["/jobs/".Length..], out var id)
            ? id
            : null;

    private static T Read<T>(string body, T? empty = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(body))
            return empty ?? throw new ArgumentException("The request needs a JSON body.");
        return JsonSerializer.Deserialize<T>(body, Json) ?? empty ??
               throw new ArgumentException("The request needs a JSON body.");
    }

    private static async Task<string> ReadBodyAsync(HttpListenerRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasEntityBody)
            return string.Empty;
        if (request.ContentLength64 > MaximumBodyBytes)
            throw new InvalidDataException($"The body is larger than {MaximumBodyBytes} bytes.");
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static async Task WriteAsync(HttpListenerResponse response, int status, string text, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        response.StatusCode = status;
        response.ContentType = contentType;
        response.Headers["Cache-Control"] = "no-store";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
    }

    private static string Page()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(PageResource)
                           ?? throw new InvalidOperationException($"The resource {PageResource} is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
