# Integration Testing

## Approach

Integration tests run against a **real in-process Kestrel server** (`ApiServerFixture`) bound to `127.0.0.1:0` (random free port). This exercises the genuine HTTP stack — sockets, headers, chunking, form parsing, ASP.NET's real ProblemDetails serialization — with zero external dependencies, so the suite runs anywhere `dotnet test` runs.

## What the fixture serves

| Endpoint | Purpose |
|---|---|
| `GET /echo` | correlation-ID and User-Agent propagation proof |
| `GET /flaky/{key}` | fails twice with 503, then succeeds — proves default retries (per-key counters keep tests isolated) |
| `GET /orders?cursor=` | 3-page cursor pagination |
| `POST /token` + `GET /secure` | full OAuth client-credentials round trip with token-issue counting |
| `GET /problem` | real `Results.ValidationProblem` → `ProblemDocument` parsing |
| `GET /download`, `POST /upload` | 128 KB streaming download, multipart round trip |

## Writing a new integration test

```csharp
public class MyTests(ApiServerFixture fixture) : IClassFixture<ApiServerFixture>
{
    [Fact]
    public async Task MyScenario()
    {
        var services = new ServiceCollection();
        services.AddKorasApiClient("integration", o => o.BaseAddress = fixture.BaseAddress)
            .ConfigureResilience(r => r.Retry.Delay = TimeSpan.FromMilliseconds(20)); // keep retries fast
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IApiClientFactory>().CreateClient("integration");
        // ...
    }
}
```

Guidelines: unique keys/routes per test for stateful endpoints; shrink retry delays; never depend on the public internet (that's what samples are for).
