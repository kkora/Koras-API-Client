# FAQ

**Why doesn't `SendAsync` throw on 404/500?**
Errors-as-data ([ADR-0003](../architecture/decision-records/adr-0003-errors-as-data.md)): integration code branches on failure kinds constantly, and exception-driven control flow makes that miserable. `EnsureSuccess()`/`GetValueOrThrow()` restore throw-style when you want it — one exception type, `ApiClientException`.

**How is this different from Refit / RestSharp / Flurl?**
See the [competitive analysis](../product/competitive-analysis.md). Short version: those give you a nicer request syntax; Koras additionally standardizes auth flows, resilience, pagination, caching, telemetry, and a normalized error taxonomy — composed on `HttpClientFactory`, with a first-class testing package.

**Does it support .NET Framework / netstandard2.0?**
No ([ADR-0002](../architecture/decision-records/adr-0002-target-frameworks.md)). net8.0+ only.

**Can I use my own Polly pipeline?**
Yes — tune the standard one via `ConfigureResilience`, or add custom pipelines on `builder.HttpClientBuilder`.

**XML APIs? Protobuf?**
Send anything via `WithContent(HttpContent)` and receive raw via `SendAsync<string>`/`DownloadAsync`. JSON is the only *automatic* (de)serialization in core.

**Is `IApiClient` a singleton? Do I need to cache clients from the factory?**
Clients are cheap, thread-safe wrappers; `HttpClientFactory` manages the expensive handlers underneath. Inject once or create per use — both are fine.

**Where do retries happen relative to auth?**
Pipeline order: caching → resilience → auth → transport. Each retry re-attaches a (possibly refreshed) token; cache hits skip everything.

**Azure managed identity?**
Five-line adapter over `IAccessTokenProvider` today ([authentication.md](../features/authentication.md)); official `Koras.ApiClient.Azure` package at 0.5.0.

**How do I mock this in unit tests?**
Don't mock `IApiClient` — use `MockApiHandler` + `KorasApiClientTestFactory` and exercise the real pipeline ([testing-support.md](../features/testing-support.md)).
