using System.Net;
using System.Net.Sockets;
using Prevue.Commands;
using Prevue.Commands.Control;
using PrevueCommander.Model;
using PrevueCommander.Model.PlaybookCommands;
using PrevueCommander.Server;
using Xunit;

namespace PrevueCommander.Tests;

public class ServerTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 18, 10, 0);

    /// <summary>Records the batches that the jobs send.</summary>
    private sealed class FakeSink : ICommandSink
    {
        public List<IReadOnlyList<BaseCommand>> Batches { get; } = [];

        public Task SendBatchAsync(IReadOnlyList<BaseCommand> commands, Action<BaseCommand>? sent,
            CancellationToken cancellationToken)
        {
            lock (Batches)
                Batches.Add(commands);
            foreach (var command in commands)
                sent?.Invoke(command);
            return Task.CompletedTask;
        }
    }

    private sealed class Server
    {
        public Server(IEnumerable<BaseCommand>? playbookCommands = null, bool controlLine = true)
        {
            var session = new SessionTemplate();
            session.Remember(playbookCommands ?? []);
            Jobs = new JobQueue(Sink, _ => TimeSpan.FromSeconds(1), () => Now, TextWriter.Null);
            Hold = new TopHalfHold(Jobs, () => Now, TextWriter.Null, TestContext.Current.CancellationToken);
            var parts = new ServerParts(() => new PlaybookContext(Now, Path.GetTempPath(), TextWriter.Null),
                session,
                commands =>
                {
                    if (!controlLine && commands.Any(c => c.Line == SerialLine.Control))
                        throw new InvalidOperationException("No control line.");
                },
                _ => (false, 0), 110);
            Control = new ControlServer(parts, Jobs, Hold, () => Now, TextWriter.Null);
        }

        public FakeSink Sink { get; } = new();
        public JobQueue Jobs { get; }
        public TopHalfHold Hold { get; }
        public ControlServer Control { get; }

        public Task<(int Status, object Answer)> Post(string path, string body = "") =>
            Control.HandleAsync("POST", path, body, TestContext.Current.CancellationToken);

        /// <summary>Sends the queued jobs, and returns the commands of each batch.</summary>
        public async Task<List<IReadOnlyList<BaseCommand>>> SendAsync()
        {
            using var stop = new CancellationTokenSource();
            var run = Jobs.RunAsync(stop.Token);
            for (var i = 0; i < 100 && Jobs.Waiting() != (0, 0); i++)
                await Task.Delay(10, TestContext.Current.CancellationToken);
            await stop.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            return Sink.Batches;
        }
    }

    private static string Hex(IEnumerable<BaseCommand> commands) =>
        string.Join(" ", commands.Select(c => Convert.ToHexString(c.Render())));

    [Theory]
    [InlineData("""{"title": "Seinfeld", "brush": "AT"}""")]
    [InlineData("""{"right": {"title": "Seinfeld", "channels": "*", "brush": "AT"}}""")]
    public async Task Promo_WithTheBodyOfEsquire_SendsThePromoPackets(string body)
    {
        var server = new Server();

        Assert.Equal(202, (await server.Post("/prevue/ctrl/promo", body)).Status);

        var batch = Assert.Single(await server.SendAsync());
        Assert.Equal(Hex(Promo.Commands(new PromoBox("Seinfeld", "*", "AT"), null)), Hex(batch));
    }

    [Fact]
    public async Task Promo_WithTwoBoxes_TriesTheFirstBox()
    {
        var server = new Server();

        await server.Post("/prevue/ctrl/promo",
            """{"right": {"title": "News"}, "left": {"title": "Seinfeld"}, "first": "left"}""");

        var expected = Promo.Commands(new PromoBox("News"), new PromoBox("Seinfeld"), rightFirst: false);
        Assert.Equal(Hex(expected), Hex(Assert.Single(await server.SendAsync())));
    }

    [Theory]
    [InlineData("/prevue/ctrl/clear", "0133")]
    [InlineData("/prevue/ctrl/logo", "0144")]
    [InlineData("/prevue/ctrl/ad", "0C")]
    [InlineData("/prevue/ctrl/weather", "015730")]
    public async Task TopHalfCommands_SendOnePacket(string path, string start)
    {
        var server = new Server();

        Assert.Equal(202, (await server.Post(path)).Status);

        var packet = Assert.Single(Assert.Single(await server.SendAsync()));
        Assert.StartsWith(start, Convert.ToHexString(packet.Render()));
    }

    [Fact]
    public async Task Packets_SendTheRawPackets()
    {
        var server = new Server();

        await server.Post("/prevue/ctrl/packets", """[{"type": 17, "body": "Seinfeld"}, {"type": 1, "body": "1*"}]""");

        Assert.Equal(Hex([new RawControlCommand(17, "Seinfeld"), new RawControlCommand(1, "1*")]),
            Hex(Assert.Single(await server.SendAsync())));
    }

    [Theory]
    [InlineData("/prevue/ctrl/promo", "{}")]
    [InlineData("/prevue/ctrl/promo", "{not json")]
    [InlineData("/prevue/ctrl/packets", """[{"type": 30, "body": ""}]""")]
    [InlineData("/prevue/data/local-ads", "")]
    [InlineData("/prevue/data/weather", "{}")]
    [InlineData("/prevue/commands", "- !!unknownCommand {}")]
    [InlineData("/prevue/commands", "")]
    [InlineData("/prevue/ctrl/hold", """{"intervalSeconds": 200}""")]
    public async Task BadRequests_Get400_AndSendNothing(string path, string body)
    {
        var server = new Server();

        var (status, answer) = await server.Post(path, body);

        Assert.Equal(400, status);
        Assert.IsType<ErrorAnswer>(answer);
        Assert.Empty(server.Jobs.Jobs());
    }

    [Fact]
    public async Task ControlCommands_WithoutAControlLine_Get400()
    {
        var server = new Server(controlLine: false);
        Assert.Equal(400, (await server.Post("/prevue/ctrl/clear")).Status);
    }

    [Fact]
    public async Task UnknownPath_Gets404() =>
        Assert.Equal(404, (await new Server().Post("/nothing")).Status);

    [Fact]
    public async Task LocalAds_DryRun_ReturnsThePreview_AndSendsNothing()
    {
        var server = new Server();

        var (status, answer) = await server.Post("/prevue/data/local-ads",
            """{"ads": ["ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghij"], "dryRun": true}""");

        Assert.Equal(200, status);
        var json = System.Text.Json.JsonSerializer.Serialize(answer, ControlServer.Json);
        Assert.Contains("|ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcd|", json);
        Assert.Contains("ESQ continues it on line 2", json);
        Assert.Empty(server.Jobs.Jobs());
    }

    [Fact]
    public async Task LocalAds_GoInTheSessionOfThePlaybook()
    {
        var server = new Server([new AddressCommand("12345"), new ConfigurationCommand(), new SaveCommand()]);

        Assert.Equal(202, (await server.Post("/prevue/data/local-ads", """{"ads": ["One", "Two"]}""")).Status);

        var batch = Assert.Single(await server.SendAsync());
        Assert.Equal([typeof(AddressCommand), typeof(ConfigurationCommand), typeof(ResetLocalAdsCommand),
            typeof(LocalAdCommand), typeof(LocalAdCommand), typeof(SaveCommand), typeof(BoxOffCommand)],
            batch.Select(c => c.GetType()));
        Assert.Equal(Hex([new AddressCommand("12345")]), Hex([batch[0]]));
    }

    [Fact]
    public async Task Weather_SendsASession_APause_AndTheShowCommand()
    {
        var server = new Server();

        await server.Post("/prevue/data/weather",
            """{"status": {"label": "DETROIT", "text": "Detroit"}, "text": {"label": "DETROIT", "lines": ["Sunny"]}}""");

        var batch = Assert.Single(await server.SendAsync());
        Assert.Equal([typeof(AddressCommand), typeof(ConfigurationCommand), typeof(WeatherStatusCommand),
            typeof(WeatherTextCommand), typeof(BoxOffCommand), typeof(PauseCommand), typeof(ShowWeatherCommand)],
            batch.Select(c => c.GetType()));
    }

    [Fact]
    public async Task Weather_WithAForecast_ShowsTheForecast()
    {
        var server = new Server();

        await server.Post("/prevue/data/weather",
            """{"status": {"label": "DETROIT"}, "forecast": {"days": [{"icon": 2, "high": 81, "low": 51}]}}""");

        var batch = Assert.Single(await server.SendAsync());
        Assert.IsType<WeatherForecastCommand>(batch[3]);
        Assert.True(Assert.IsType<ShowWeatherCommand>(batch[^1]).Forecast);
    }

    [Fact]
    public async Task Commands_ReadAYamlList_AndSendItAsWritten()
    {
        var server = new Server();

        var (status, _) = await server.Post("/prevue/commands", "- !!clearTopHalf {}\n- !!pause\n  seconds: 1\n");

        Assert.Equal(202, status);
        Assert.Equal([typeof(ClearTopHalfCommand), typeof(PauseCommand)],
            Assert.Single(await server.SendAsync()).Select(c => c.GetType()));
    }

    [Fact]
    public async Task Commands_ReadAPlaybookWithCommands()
    {
        var server = new Server();

        Assert.Equal(202, (await server.Post("/prevue/commands", "commands:\n- !!showLogo {}\n")).Status);
        Assert.IsType<ShowLogoCommand>(Assert.Single(Assert.Single(await server.SendAsync())));
    }

    [Fact]
    public async Task Hold_SendsAClear_AndStops()
    {
        var server = new Server();

        var (status, answer) = await server.Post("/prevue/ctrl/hold", """{"minutes": 10}""");

        Assert.Equal(202, status);
        var info = Assert.IsType<HoldInfo>(answer);
        Assert.True(info.Active);
        Assert.Equal(Now.AddMinutes(10), info.Until);
        Assert.Equal(150, info.IntervalSeconds);
        for (var i = 0; i < 100 && server.Jobs.Jobs().Count == 0; i++)
            await Task.Delay(10, TestContext.Current.CancellationToken);
        Assert.Equal("Keep the top half clear", Assert.Single(server.Jobs.Jobs()).Name);

        var stop = await server.Control.HandleAsync("DELETE", "/prevue/ctrl/hold", "",
            TestContext.Current.CancellationToken);
        Assert.Equal(200, stop.Status);
        Assert.False(server.Hold.Info.Active);
        Assert.Equal(409, (await server.Control.HandleAsync("DELETE", "/prevue/ctrl/hold", "",
            TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task AQueuedJob_CanBeCancelled_AndIsNotSent()
    {
        var server = new Server();
        await server.Post("/prevue/ctrl/clear");
        var id = Assert.Single(server.Jobs.Jobs()).Id;

        var cancel = await server.Control.HandleAsync("DELETE", $"/jobs/{id}", "",
            TestContext.Current.CancellationToken);

        Assert.Equal(200, cancel.Status);
        Assert.Equal(JobState.Cancelled, server.Jobs.Find(id)!.State);
        Assert.Empty(await server.SendAsync());
        Assert.Equal(409, (await server.Control.HandleAsync("DELETE", $"/jobs/{id}", "",
            TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task ControlLineState_HasTheFieldsOfEsquire()
    {
        var server = new Server();
        await server.Post("/prevue/ctrl/clear");

        var (status, answer) = await server.Control.HandleAsync("GET", "/prevue/ctrl", "",
            TestContext.Current.CancellationToken);

        Assert.Equal(200, status);
        var json = System.Text.Json.JsonSerializer.Serialize(answer, ControlServer.Json);
        Assert.Contains("\"queued\": 4", json);
        Assert.Contains("\"seconds\": 0.4", json);
        Assert.Contains("\"sent\": 0", json);
    }

    [Fact]
    public async Task Refresh_WithoutAGuideImport_Gets409() =>
        Assert.Equal(409, (await new Server().Post("/prevue/guide/refresh")).Status);

    [Theory]
    [InlineData("127.0.0.1", "http://127.0.0.1:8095/", "http://localhost:8095/")]
    [InlineData("0.0.0.0", "http://+:8095/")]
    [InlineData("192.168.0.5", "http://192.168.0.5:8095/")]
    public void Prefixes_CoverTheNamesOfTheHost(string host, params string[] prefixes) =>
        Assert.Equal(prefixes, ControlServer.Prefixes(host, 8095));

    [Fact]
    public async Task HttpServer_ServesThePage_AndTheApi()
    {
        var server = new Server();
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var run = server.Control.RunAsync("127.0.0.1", port, stop.Token);
        using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}/") };

        var page = await client.GetStringAsync("/", TestContext.Current.CancellationToken);
        var clear = await client.PostAsync("/prevue/ctrl/clear", null, TestContext.Current.CancellationToken);
        var missing = await client.GetAsync("/nothing", TestContext.Current.CancellationToken);

        Assert.Contains("<title>Prevue Commander</title>", page);
        Assert.Equal(HttpStatusCode.Accepted, clear.StatusCode);
        Assert.Contains("\"name\": \"Clear the top half\"",
            await clear.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        // A POST without Content-Length, as "curl -X POST" sends it, gets 411 and queues nothing.
        using (var raw = new TcpClient())
        {
            await raw.ConnectAsync(IPAddress.Loopback, port, TestContext.Current.CancellationToken);
            var stream = raw.GetStream();
            await stream.WriteAsync("POST /prevue/ctrl/clear HTTP/1.1\r\nHost: localhost\r\n\r\n"u8.ToArray(),
                TestContext.Current.CancellationToken);
            using var reader = new StreamReader(stream);
            Assert.Contains("411", await reader.ReadLineAsync(TestContext.Current.CancellationToken));
        }

        // Give a wrong handler the time to queue a job.
        await Task.Delay(200, TestContext.Current.CancellationToken);
        Assert.Single(server.Jobs.Jobs());

        await stop.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
    }

    [Fact]
    public async Task SerialLines_DoNotMixTwoBatchesOfOneLine()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var configuration = new Configuration { Port = port, BaudRate = 0, CommandDelay = 5 };
        await using var lines = new SerialLines(configuration, TextWriter.Null);

        var accept = listener.AcceptTcpClientAsync(TestContext.Current.CancellationToken).AsTask();
        List<BaseCommand> first = [.. Enumerable.Range(0, 5).Select(_ => new AddressCommand("11111"))];
        List<BaseCommand> second = [.. Enumerable.Range(0, 5).Select(_ => new AddressCommand("22222"))];
        await Task.WhenAll(lines.SendBatchAsync(first, null, TestContext.Current.CancellationToken),
            lines.SendBatchAsync(second, null, TestContext.Current.CancellationToken));
        await lines.DisposeAsync();

        using var client = await accept;
        var received = new MemoryStream();
        await client.GetStream().CopyToAsync(received, TestContext.Current.CancellationToken);
        listener.Stop();

        var hex = Convert.ToHexString(received.ToArray());
        var one = Hex(first).Replace(" ", "");
        var two = Hex(second).Replace(" ", "");
        Assert.True(hex == one + two || hex == two + one, "The two batches were mixed.");
        Assert.Equal(lines.BytesSent(SerialLine.Data), received.Length);
    }
}
