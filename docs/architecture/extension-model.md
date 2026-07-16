# Extension Model

Koras.ApiClient is extended at five seams. All are additive; none require forking internals.

## 1. Token providers (`IAccessTokenProvider`)

Any auth scheme that produces bearer tokens plugs in here — Azure managed identity, custom STS, cached Kerberos-to-JWT exchanges:

```csharp
public sealed class MyStsTokenProvider : IAccessTokenProvider
{
    public ValueTask<AccessToken> GetTokenAsync(bool forceRefresh, CancellationToken ct) => ...;
}
services.AddKorasApiClient("partner", ...).WithBearerToken<MyStsTokenProvider>();
```

## 2. Message handlers (`DelegatingHandler`)

`IKorasApiClientBuilder.HttpClientBuilder` exposes the underlying `IHttpClientBuilder`, so any standard handler composes:

```csharp
builder.HttpClientBuilder.AddHttpMessageHandler(() => new MySigningHandler());
```

This is the seam for request signing, custom auth, and wire mocking.

## 3. Resilience pipeline

`ConfigureResilience(Action<HttpStandardResilienceOptions>)` tunes retry/circuit-breaker/timeouts; advanced users can add extra Polly pipelines via the raw `IHttpClientBuilder`.

## 4. Serialization

`KorasApiClientOptions.JsonSerializerOptions` (defaults to `JsonSerializerDefaults.Web`) controls all JSON handling; per-call override via `WithJsonBody(body, options)`. Non-JSON payloads use `WithContent(HttpContent)` and `DownloadAsync(stream)` — the framework never forces JSON.

## 5. Pagination selectors

`PaginateAsync` takes delegates for request-building and page-extraction, so any pagination dialect (cursor, link header, page/offset) maps onto `Page<T>` without framework changes.

## Patterns used

- **Options pattern** everywhere (`KorasApiClientOptions`, named per client, `IValidateOptions` at startup).
- **Builder pattern** for DI (`IKorasApiClientBuilder`) and requests (`ApiRequestBuilder`).
- **Factory pattern** for named clients (`IApiClientFactory` mirroring `IHttpClientFactory`).
- **No service locator, no statics** (the only statics are telemetry instrument definitions, which is the .NET metrics idiom).
