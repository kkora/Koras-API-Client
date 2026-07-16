using BenchmarkDotNet.Attributes;
using Koras.ApiClient.Testing;

namespace Koras.ApiClient.Benchmarks;

public sealed record Widget(int Id, string Name);

/// <summary>
/// Hot-path benchmarks over an in-memory transport: full logical send (URI composition,
/// correlation, telemetry, deserialization, error normalization) and pure request building.
/// </summary>
[MemoryDiagnoser]
public class SendBenchmarks
{
    private IApiClient _client = null!;

    [GlobalSetup]
    public void Setup()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/widgets*").RespondWithJson(new Widget(1, "benchmark"));
        handler.When(HttpMethod.Get, "/missing").RespondWith(System.Net.HttpStatusCode.NotFound, "{}");
        _client = KorasApiClientTestFactory.Create(handler);
    }

    [Benchmark(Description = "GET + JSON deserialize (success path)")]
    public Task<ApiResponse<Widget>> SendAndDeserialize() =>
        _client.Get("widgets/1").WithQuery("expand", "details").SendAsync<Widget>();

    [Benchmark(Description = "GET → 404 error normalization")]
    public Task<ApiResponse<Widget>> SendAndNormalizeError() =>
        _client.Get("missing").SendAsync<Widget>();

    [Benchmark(Description = "Request building only (no send)")]
    public ApiRequest BuildRequest() =>
        _client.Get("widgets")
            .WithQuery("page", 1)
            .WithQuery("size", 50)
            .WithHeader("X-Requested-With", "benchmark")
            .Build();
}
