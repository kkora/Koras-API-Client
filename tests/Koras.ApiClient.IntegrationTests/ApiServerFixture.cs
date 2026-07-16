using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Koras.ApiClient.IntegrationTests;

public sealed record Widget(int Id, string Name);

public sealed record Order(string Id);

public sealed record OrderPage(IReadOnlyList<Order> Items, string? Next);

public sealed record EchoResponse(string CorrelationId, string UserAgent);

public sealed record UploadResult(int Files, int Fields);

/// <summary>An in-process Kestrel API exercising the full client pipeline over real HTTP.</summary>
public sealed class ApiServerFixture : IAsyncLifetime
{
    private readonly ConcurrentDictionary<string, int> _flakyCounters = new();
    private WebApplication? _app;
    private int _tokenRequests;

    public Uri BaseAddress { get; private set; } = null!;

    public int TokenRequests => Volatile.Read(ref _tokenRequests);

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();

        app.MapGet("/echo", (HttpRequest request) => Results.Json(new EchoResponse(
            request.Headers["X-Correlation-ID"].ToString(),
            request.Headers.UserAgent.ToString())));

        app.MapGet("/flaky/{key}", (string key) =>
        {
            var attempt = _flakyCounters.AddOrUpdate(key, 1, (_, value) => value + 1);
            return attempt <= 2 ? Results.StatusCode(StatusCodes.Status503ServiceUnavailable) : Results.Json(new Widget(1, "recovered"));
        });

        app.MapGet("/orders", (string? cursor) => cursor switch
        {
            null or "" => Results.Json(new OrderPage([new Order("o1"), new Order("o2")], "c2")),
            "c2" => Results.Json(new OrderPage([new Order("o3")], "c3")),
            "c3" => Results.Json(new OrderPage([new Order("o4")], null)),
            _ => Results.NotFound(),
        });

        app.MapPost("/token", async (HttpRequest request) =>
        {
            var form = await request.ReadFormAsync();
            if (form["grant_type"] != "client_credentials" || form["client_id"] != "integration-id" || form["client_secret"] != "integration-secret")
            {
                return Results.BadRequest(new { error = "invalid_client" });
            }

            Interlocked.Increment(ref _tokenRequests);
            return Results.Json(new { access_token = "integration-token", expires_in = 3600, token_type = "Bearer" });
        });

        app.MapGet("/secure", (HttpRequest request) =>
            request.Headers.Authorization.ToString() == "Bearer integration-token"
                ? Results.Json(new Widget(42, "classified"))
                : Results.Unauthorized());

        app.MapGet("/problem", () => Results.ValidationProblem(
            new Dictionary<string, string[]> { ["name"] = ["Name is required."] },
            title: "Validation failed"));

        app.MapGet("/download", () => Results.Bytes(new byte[128 * 1024], "application/octet-stream"));

        app.MapPost("/upload", async (HttpRequest request) =>
        {
            var form = await request.ReadFormAsync();
            return Results.Json(new UploadResult(form.Files.Count, form.Count));
        });

        await app.StartAsync();
        _app = app;
        BaseAddress = new Uri(app.Urls.First() + "/");
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
