<p align="center">
  <img src="assets/icon.png" width="96" alt="Koras.ApiClient logo" />
</p>

<h1 align="center">Koras.ApiClient</h1>

<p align="center"><b>A resilient, observable, secure-by-default HTTP API client framework for enterprise .NET.</b></p>

<p align="center">
  <a href="https://github.com/KKORA/koras-api-client/actions/workflows/build.yml"><img src="https://github.com/KKORA/koras-api-client/actions/workflows/build.yml/badge.svg" alt="build" /></a>
  <a href="https://github.com/KKORA/koras-api-client/actions/workflows/test.yml"><img src="https://github.com/KKORA/koras-api-client/actions/workflows/test.yml/badge.svg" alt="test" /></a>
  <a href="https://www.nuget.org/packages/Koras.ApiClient"><img src="https://img.shields.io/nuget/v/Koras.ApiClient.svg" alt="NuGet" /></a>
  <a href="https://www.nuget.org/packages/Koras.ApiClient"><img src="https://img.shields.io/nuget/dt/Koras.ApiClient.svg" alt="downloads" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="MIT" /></a>
  <img src="https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4" alt=".NET 8/9/10" />
</p>

Teams rebuild the same `HttpClient` plumbing in every service: authentication handlers, retry policies, pagination loops, caching, correlation IDs, and inconsistent error handling. **Koras.ApiClient** composes all of it — on top of `HttpClientFactory` and `Microsoft.Extensions.Http.Resilience`, never instead of them — behind one line of DI registration and a fluent request API.

## Key features

- 🔁 **Resilient by default** — bounded retries with jitter, circuit breaker, timeouts, `Retry-After` handling; on for every client, tunable via `ConfigureResilience`
- 🧾 **Errors as data** — every failure (network, timeout, 4xx/5xx, RFC 9457 ProblemDetails, bad JSON) becomes a categorized `ApiError`; exceptions are opt-in with a single type
- 🔐 **Auth included** — API key, bearer tokens (`IAccessTokenProvider` seam, managed-identity-ready), OAuth 2.0 client credentials with cached single-flight refresh
- 📄 **Pagination as `IAsyncEnumerable<T>`** — cursor & page-number strategies, bounded pages, full cancellation
- ⚡ **Response caching** — opt-in per request (`WithCache`) or per client, memory-backed with size limits
- 🔎 **Observability built-in** — structured redacted logs, correlation IDs, `ActivitySource` spans and `Meter` metrics ready for OpenTelemetry
- 🧪 **First-class testing** — `Koras.ApiClient.Testing` scripts responses and records requests through the *real* pipeline
- 📦 **Lean packages** — `Abstractions` (contracts, zero third-party deps) · `Koras.ApiClient` (framework, Microsoft-only deps) · `Testing`

## Installation

```bash
dotnet add package Koras.ApiClient
dotnet add package Koras.ApiClient.Testing    # test projects
```

Supports **.NET 8, 9, and 10**.

## Five-minute quick start

```csharp
// 1. Register (Program.cs)
builder.Services.AddKorasApiClient(options =>
    options.BaseAddress = new Uri("https://api.example.com/"));

// 2. Inject and call
public sealed record Todo(int Id, string Title, bool Completed);

public sealed class TodoService(IApiClient api)
{
    public async Task<Todo?> GetAsync(int id, CancellationToken ct)
    {
        var response = await api.Get($"todos/{id}").SendAsync<Todo>(ct);

        if (response.Error?.Category == ApiErrorCategory.NotFound)
            return null;                        // failures are data

        return response.GetValueOrThrow();      // exceptions opt-in: one type, ApiClientException
    }
}
```

That call already has retries, a circuit breaker, timeouts, a correlation ID, redacted structured logs, and an OpenTelemetry-ready span. Nothing else to configure.

## Authentication in one line each

```csharp
services.AddKorasApiClient("vendor", o => o.BaseAddress = new Uri("https://api.vendor.com/"))
    .WithApiKey(k => k.ApiKey = config["Vendor:ApiKey"]!);                 // header-based API key

services.AddKorasApiClient("partner", o => o.BaseAddress = new Uri("https://api.partner.com/"))
    .WithClientCredentials(oauth =>                                        // OAuth2, cached + single-flight
    {
        oauth.TokenEndpoint = new Uri("https://login.partner.com/oauth/token");
        oauth.ClientId = config["Partner:ClientId"]!;
        oauth.ClientSecret = config["Partner:ClientSecret"]!;
        oauth.Scopes.Add("read:orders");
    });
```

## Configuration from appsettings.json

```json
{ "Clients": { "Vendor": { "BaseAddress": "https://api.vendor.com/", "Timeout": "00:00:20" } } }
```

```csharp
services.AddKorasApiClient("vendor", builder.Configuration.GetSection("Clients:Vendor"));
```

Options are validated at startup — misconfiguration fails the host boot, not your 2 a.m. request.

## Pagination, uploads, caching

```csharp
await foreach (var order in client.PaginateAsync<OrderPage, Order>(
    (c, cursor) => c.Get("orders").WithQuery("cursor", cursor),
    page => new Page<Order>(page.Items, page.NextCursor), cancellationToken: ct)) { ... }

await client.Post("documents")
    .WithMultipart(m => m.AddFile("file", stream, "report.pdf", "application/pdf"))
    .SendAsync<DocumentCreated>(ct);

var countries = await client.Get("reference/countries")
    .WithCache(TimeSpan.FromHours(1))
    .SendAsync<List<Country>>(ct);
```

## Testing without a network

```csharp
var handler = new MockApiHandler();
handler.When(HttpMethod.Get, "/todos/1").RespondWithJson(new Todo(1, "write tests", false));
handler.When(HttpMethod.Get, "/down").Throws(new HttpRequestException());

IApiClient client = KorasApiClientTestFactory.Create(handler);   // real pipeline, fake transport
```

## Documentation & samples

📚 [Documentation index](docs/index.md) — [quick start](docs/getting-started/quick-start.md) · [feature guides](docs/features/) · [architecture + ADRs](docs/architecture/) · [configuration reference](docs/configuration/all-options.md) · [troubleshooting & FAQ](docs/troubleshooting/faq.md)

▶️ Runnable samples: [Console](samples/Console.Sample/) (with offline mode) · [Minimal API](samples/MinimalApi.Sample/) · [Worker Service](samples/WorkerService.Sample/)

## Architecture

```
Koras.ApiClient.Testing ──► Koras.ApiClient ──► Koras.ApiClient.Abstractions ──► BCL
                             (HttpClientFactory pipeline: caching → resilience → auth)
```

Details in [docs/architecture/overview.md](docs/architecture/overview.md).

## Security & performance

Secure defaults throughout: credential-free logs (path-only URLs), bounded buffers and retries, opt-in caching, startup validation, Microsoft-only dependency graph, CodeQL + dependency review + vulnerability audits in CI, deterministic builds with Source Link. See the [threat model](docs/security/threat-model.md). Hot paths are benchmarked ([performance guide](docs/performance/performance-guide.md)); large payloads always stream.

## Versioning & support

Semantic versioning with a documented [breaking-change process](docs/api/backward-compatibility.md) and [support policy](docs/release/versioning.md). Currently **0.1.0-preview.1**.

## Contributing & support

Contributions welcome — start with [CONTRIBUTING.md](CONTRIBUTING.md). Questions → [SUPPORT.md](SUPPORT.md). Vulnerabilities → [SECURITY.md](SECURITY.md) (private reporting, please).

## License

[MIT](LICENSE) © Koras Technologies
