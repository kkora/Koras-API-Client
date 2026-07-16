# Public API Design

Design goals: predictable, IntelliSense-discoverable, minimal, strongly typed, async-first, CancellationToken-aware, DI-friendly, test-friendly, backward-compatible. Everything not listed here is `internal`.

## Koras.ApiClient.Abstractions (namespace `Koras.ApiClient`)

### `IApiClient`

Entry point consumers depend on. Thread-safe; register-once, inject-anywhere.

```csharp
public interface IApiClient
{
    string Name { get; }
    JsonSerializerOptions JsonOptions { get; }
    Task<ApiResponse> SendAsync(ApiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> SendAsync<T>(ApiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DownloadAsync(ApiRequest request, Stream destination, IProgress<long>? progress = null, CancellationToken cancellationToken = default);
}
```

- Exceptions: `ArgumentNullException` (null request/destination), `InvalidOperationException` (relative path without base address), `OperationCanceledException` (caller cancellation). Everything else is an `ApiError`.

### `IApiClientFactory`

```csharp
public interface IApiClientFactory { IApiClient CreateClient(string name); }
```

Mirrors `IHttpClientFactory`; singleton; unknown names still produce a client (options default) — consistent with `IHttpClientFactory` semantics.

### `ApiRequest` (sealed class)

Pure data: `Method`, `Path`, `QueryParameters` (multi-value list), `Headers` (case-insensitive), `Content` (`HttpContent?`), `Timeout?`, `CorrelationId?`, `CacheDuration?`, `Properties` (extension bag). Mutable until sent; built by `ApiRequestBuilder`, constructible directly for advanced scenarios.

### `ApiResponse` / `ApiResponse<T>`

```csharp
public class ApiResponse
{
    bool IsSuccess { get; }              // Error is null
    HttpStatusCode? StatusCode { get; }  // null on transport failure
    ApiError? Error { get; }
    IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }
    string? CorrelationId { get; }
    void EnsureSuccess();                // throws ApiClientException
}
public sealed class ApiResponse<T> : ApiResponse
{
    T? Value { get; }
    T GetValueOrThrow();
    TResult Match<TResult>(Func<T?, TResult> onSuccess, Func<ApiError, TResult> onFailure);
}
```

### `ApiError` (sealed record), `ApiErrorCategory` (enum), `ProblemDocument` (sealed record), `ApiClientException`

See `docs/architecture/error-model.md` for the category table. `ApiErrorCategory` reserves `Unknown = 0` for additive evolution.

### `IAccessTokenProvider` / `AccessToken`

```csharp
public interface IAccessTokenProvider { ValueTask<AccessToken> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken); }
public readonly record struct AccessToken(string Value, DateTimeOffset ExpiresOn);
```

Providers must be thread-safe (registered as singletons).

### `Page<T>`

```csharp
public sealed record Page<T>(IReadOnlyList<T> Items, string? NextCursor = null, long? TotalCount = null);
```

### `KorasApiClientTelemetry`

Constants only: `ActivitySourceName`, `MeterName` (both `"Koras.ApiClient"`).

## Koras.ApiClient (core)

### Fluent layer (namespace `Koras.ApiClient`)

- `ApiClientExtensions`: `Get/Post/Put/Patch/Delete/Head(this IApiClient, string path)` → `ApiRequestBuilder`; conveniences `GetFromJsonAsync<T>`, `PostJsonAsync<TRequest,TResponse>`.
- `ApiRequestBuilder` (sealed; not thread-safe): `WithQuery(name, value)` (string or invariant-formatted object), `WithHeader`, `WithCorrelationId`, `WithTimeout`, `WithCache(TimeSpan)`, `WithJsonBody<T>(body, options?)`, `WithFormUrlEncoded`, `WithMultipart(Action<MultipartBuilder>)`, `WithContent(HttpContent)`, `Build()`, terminals `SendAsync(ct)`, `SendAsync<T>(ct)`, `DownloadAsync(stream, progress?, ct)`.
- `MultipartBuilder`: `AddField(name, value)`, `AddFile(name, stream, fileName, contentType?)`.
- `ApiClientPaginationExtensions` (namespace `Koras.ApiClient`): `PaginateAsync<TPage,TItem>`, `PaginateByPageNumberAsync<TPage,TItem>` returning `IAsyncEnumerable<TItem>`; both take `maxPages` guards (default 1000).

### `KorasApiClient` (sealed, public ctor for advanced/testing use)

`KorasApiClient(string name, HttpClient httpClient, KorasApiClientOptions options, ILogger<KorasApiClient>? logger = null)`. Thread-safe.

### Options (namespace `Koras.ApiClient`)

`KorasApiClientOptions`: `BaseAddress`, `Timeout` (default 30 s), `UserAgent`, `DefaultHeaders`, `CorrelationHeaderName` (default `X-Correlation-ID`), `EnableCorrelationId` (default true), `JsonSerializerOptions` (Web defaults), `CaptureErrorResponseBody` (true), `MaxErrorResponseBodyLength` (4096), `Caching` (`ResponseCachingOptions`: `Enabled`, `DefaultDuration` 60 s, `MaxEntrySizeBytes` 1 MB).

### Authentication (namespace `Koras.ApiClient.Authentication`)

`ApiKeyAuthenticationOptions` (`ApiKey`, `HeaderName`, `Location`, `QueryParameterName`), `ApiKeyLocation` enum, `ClientCredentialsOptions` (`TokenEndpoint`, `ClientId`, `ClientSecret`, `Scopes`, `AdditionalParameters`, `RefreshSkew`), `StaticTokenProvider`, `DelegateTokenProvider`, `ClientCredentialsTokenProvider`.

### DI (namespace `Microsoft.Extensions.DependencyInjection` per ecosystem convention)

- `KorasApiClientServiceCollectionExtensions.AddKorasApiClient(services, name, configure?)`, overloads: `(services, configure)` for the default client (also registers injectable `IApiClient`), `(services, name, IConfiguration)`.
- `IKorasApiClientBuilder` (`Name`, `Services`, `HttpClientBuilder`) + `KorasApiClientBuilderExtensions`: `WithApiKey`, `WithBearerToken<TProvider>`, `WithBearerToken(factory)`, `WithStaticBearerToken`, `WithClientCredentials`, `WithResponseCaching`, `ConfigureResilience`, `AddTypedClient<TClient>`.

## Koras.ApiClient.Testing (namespace `Koras.ApiClient.Testing`)

`MockApiHandler` (`When(method, path)`, `Requests`, unmatched → 404 or strict-throw mode), `MockRequestSetup` (`RespondWith`, `RespondWithJson<T>`, `RespondWithProblem`, `Throw`, sequencing by repeated calls), `RecordedRequest`, `KorasApiClientTestFactory.Create(handler, configure?, name?)`.

## Anti-patterns deliberately avoided

No static state (telemetry instruments excepted, per .NET idiom), no service locator, no hidden network calls (nothing dials out before a terminal method), no surprising defaults (resilience-on is documented loudly), no boolean overload pairs (`forceRefresh` is a named semantic parameter), no third-party types in the public surface except intentional `Microsoft.Extensions` seams (`IHttpClientBuilder`, `HttpStandardResilienceOptions`, `IConfiguration`).
