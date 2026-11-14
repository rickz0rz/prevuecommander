using System.IO.Compression;
using System.Net;
using System.Text;
using PrevueCommander.Model.PlaybookCommands;
using PrevueCommander.XmlTv;
using Xunit;

namespace PrevueCommander.Tests;

public class XmlTvSourceTests
{
    private const string Xml = """
        <?xml version="1.0"?>
        <tv>
          <channel id="c1"><display-name>2 TESTA</display-name></channel>
          <programme start="20261113180000 -0500" stop="20261113183000 -0500" channel="c1"><title>News</title></programme>
        </tv>
        """;

    private static byte[] Gzip(string text)
    {
        var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
            gzip.Write(Encoding.UTF8.GetBytes(text));
        return output.ToArray();
    }

    /// <summary>An HTTP handler that gives the same answer to each request.</summary>
    private sealed class FixedHandler(HttpStatusCode status, byte[] body) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent(body) });
        }
    }

    [Theory]
    [InlineData("http://example.com/guide.xml", true)]
    [InlineData("https://example.com/guide.xml.gz", true)]
    [InlineData("guide.xml", false)]
    [InlineData("/home/pi/guide.xml", false)]
    [InlineData("C:\\guide.xml", false)]
    [InlineData("ftp://example.com/guide.xml", false)]
    public void WebAddress_IsOnlyHttpOrHttps(string path, bool isAddress) =>
        Assert.Equal(isAddress, XmlTvGuideDataImportPlaybookCommand.WebAddress(path) != null);

    [Fact]
    public async Task Parse_ReadsGzipData()
    {
        var tv = await XmlTvGuide.ParseAsync(new MemoryStream(Gzip(Xml)), "guide.xml.gz",
            TestContext.Current.CancellationToken);
        var channel = Assert.Single(XmlTvGuide.Convert(tv));
        Assert.Equal("TESTA", channel.CallSign);
        Assert.Single(channel.Programs);
    }

    [Fact]
    public async Task Parse_GivesAClearErrorForDataThatIsNotXmlTv()
    {
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => XmlTvGuide.ParseAsync(
            new MemoryStream("not xml"u8.ToArray()), "bad.xml", TestContext.Current.CancellationToken));
        Assert.StartsWith("bad.xml is not an XMLTV file", error.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Download_ReadsTheFile(bool compressed)
    {
        var handler = new FixedHandler(HttpStatusCode.OK, compressed ? Gzip(Xml) : Encoding.UTF8.GetBytes(Xml));
        using var http = new HttpClient(handler);
        var address = new Uri("http://example.com/guide.xml");

        var tv = await XmlTvGuide.DownloadAsync(http, address, TestContext.Current.CancellationToken);

        Assert.Equal(address, handler.RequestUri);
        Assert.Equal("TESTA", Assert.Single(XmlTvGuide.Convert(tv)).CallSign);
    }

    [Fact]
    public async Task Download_GivesTheStatusOfAFailedRequest()
    {
        using var http = new HttpClient(new FixedHandler(HttpStatusCode.NotFound, []));
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => XmlTvGuide.DownloadAsync(http,
            new Uri("http://example.com/missing.xml"), TestContext.Current.CancellationToken));
        Assert.Contains("404", error.Message);
    }
}
