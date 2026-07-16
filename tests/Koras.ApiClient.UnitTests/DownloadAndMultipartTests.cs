using System.Net;
using System.Text;
using Koras.ApiClient.Testing;
using Xunit;

namespace Koras.ApiClient.UnitTests;

public class DownloadAndMultipartTests
{
    [Fact]
    public async Task Multipart_SendsFieldsAndFiles()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Post, "/upload").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler);
        using var file = new MemoryStream(Encoding.UTF8.GetBytes("file-payload"));

        var response = await client.Post("upload")
            .WithMultipart(multipart => multipart
                .AddField("description", "quarterly report")
                .AddFile("document", file, "report.txt", "text/plain"))
            .SendAsync();

        Assert.True(response.IsSuccess);
        var recorded = handler.Requests[0];
        Assert.StartsWith("multipart/form-data", recorded.Headers["Content-Type"][0]);
        Assert.Contains("quarterly report", recorded.Body);
        Assert.Contains("report.txt", recorded.Body);
        Assert.Contains("file-payload", recorded.Body);
    }

    [Fact]
    public async Task Download_StreamsBodyWithProgress()
    {
        var payload = new string('z', 100_000);
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/file").RespondWith(HttpStatusCode.OK, payload, "application/octet-stream");
        var client = KorasApiClientTestFactory.Create(handler);
        using var destination = new MemoryStream();
        var progressReports = new List<long>();

        var response = await client.Get("file").DownloadAsync(
            destination,
            new SynchronousProgress(progressReports.Add));

        Assert.True(response.IsSuccess);
        Assert.Equal(payload.Length, destination.Length);
        Assert.NotEmpty(progressReports);
        Assert.Equal(destination.Length, progressReports[^1]);
    }

    [Fact]
    public async Task Download_NonSuccessStatus_ReturnsErrorWithoutWriting()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/missing").RespondWith(HttpStatusCode.NotFound, "gone", "text/plain");
        var client = KorasApiClientTestFactory.Create(handler);
        using var destination = new MemoryStream();

        var response = await client.Get("missing").DownloadAsync(destination);

        Assert.Equal(ApiErrorCategory.NotFound, response.Error!.Category);
        Assert.Equal(0, destination.Length);
    }

    [Fact]
    public async Task Download_RejectsNullDestination()
    {
        var client = KorasApiClientTestFactory.Create(new MockApiHandler());
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Get("x").DownloadAsync(null!));
    }

    private sealed class SynchronousProgress(Action<long> report) : IProgress<long>
    {
        public void Report(long value) => report(value);
    }
}
