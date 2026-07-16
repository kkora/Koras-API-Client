using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Koras.ApiClient.IntegrationTests;

public class EndToEndTests(ApiServerFixture fixture) : IClassFixture<ApiServerFixture>
{
    private ServiceProvider BuildProvider(Action<IKorasApiClientBuilder>? configureBuilder = null)
    {
        var services = new ServiceCollection();
        var builder = services
            .AddKorasApiClient("integration", options => options.BaseAddress = fixture.BaseAddress)
            .ConfigureResilience(resilience => resilience.Retry.Delay = TimeSpan.FromMilliseconds(20));
        configureBuilder?.Invoke(builder);
        return services.BuildServiceProvider();
    }

    private static IApiClient Client(ServiceProvider provider) =>
        provider.GetRequiredService<IApiClientFactory>().CreateClient("integration");

    [Fact]
    public async Task FlakyEndpoint_SucceedsThroughDefaultRetries()
    {
        using var provider = BuildProvider();
        var client = Client(provider);

        var response = await client.Get($"flaky/{Guid.NewGuid():N}").SendAsync<Widget>();

        Assert.Equal("recovered", response.GetValueOrThrow().Name);
    }

    [Fact]
    public async Task CorrelationId_ReachesTheServer()
    {
        using var provider = BuildProvider();
        var client = Client(provider);

        var response = await client.Get("echo").SendAsync<EchoResponse>();

        var echo = response.GetValueOrThrow();
        Assert.False(string.IsNullOrEmpty(response.CorrelationId));
        Assert.Equal(response.CorrelationId, echo.CorrelationId);
        Assert.StartsWith("Koras.ApiClient/", echo.UserAgent);
    }

    [Fact]
    public async Task AspNetCoreProblemDetails_AreParsed()
    {
        using var provider = BuildProvider();
        var client = Client(provider);

        var response = await client.Get("problem").SendAsync();

        var error = response.Error!;
        Assert.Equal(ApiErrorCategory.Validation, error.Category);
        Assert.NotNull(error.Problem);
        Assert.Equal("Validation failed", error.Problem!.Title);
        Assert.Equal(["Name is required."], error.Problem.Errors!["name"]);
    }

    [Fact]
    public async Task ClientCredentials_AuthenticateAndReuseCachedToken()
    {
        using var provider = BuildProvider(builder => builder.WithClientCredentials(options =>
        {
            options.TokenEndpoint = new Uri(fixture.BaseAddress, "token");
            options.ClientId = "integration-id";
            options.ClientSecret = "integration-secret";
        }));
        var client = Client(provider);

        var first = await client.Get("secure").SendAsync<Widget>();
        var second = await client.Get("secure").SendAsync<Widget>();

        Assert.Equal("classified", first.GetValueOrThrow().Name);
        Assert.Equal("classified", second.GetValueOrThrow().Name);
        Assert.Equal(1, fixture.TokenRequests);
    }

    [Fact]
    public async Task CursorPagination_EnumeratesAllPages()
    {
        using var provider = BuildProvider();
        var client = Client(provider);

        var ids = new List<string>();
        await foreach (var order in client.PaginateAsync<OrderPage, Order>(
            (c, cursor) => c.Get("orders").WithQuery("cursor", cursor),
            page => new Page<Order>(page.Items, page.Next)))
        {
            ids.Add(order.Id);
        }

        Assert.Equal(["o1", "o2", "o3", "o4"], ids);
    }

    [Fact]
    public async Task Download_StreamsRealResponseBody()
    {
        using var provider = BuildProvider();
        var client = Client(provider);
        using var destination = new MemoryStream();
        long lastReported = 0;

        var response = await client.Get("download").DownloadAsync(
            destination,
            new ProgressCapture(value => lastReported = value));

        Assert.True(response.IsSuccess);
        Assert.Equal(128 * 1024, destination.Length);
        Assert.Equal(destination.Length, lastReported);
    }

    [Fact]
    public async Task MultipartUpload_ReachesServerIntact()
    {
        using var provider = BuildProvider();
        var client = Client(provider);
        using var file = new MemoryStream("integration file body"u8.ToArray());

        var response = await client.Post("upload")
            .WithMultipart(multipart => multipart
                .AddField("category", "reports")
                .AddFile("document", file, "report.txt", "text/plain"))
            .SendAsync<UploadResult>();

        var result = response.GetValueOrThrow();
        Assert.Equal(1, result.Files);
        Assert.Equal(1, result.Fields);
    }

    private sealed class ProgressCapture(Action<long> onReport) : IProgress<long>
    {
        public void Report(long value) => onReport(value);
    }
}
