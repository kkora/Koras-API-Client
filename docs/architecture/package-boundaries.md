# Package Boundaries

## Koras.ApiClient.Abstractions

**Contains:** `IApiClient`, `IApiClientFactory`, `ApiRequest`, `ApiResponse`, `ApiResponse<T>`, `ApiError`, `ApiErrorCategory`, `ProblemDocument`, `ApiClientException`, `IAccessTokenProvider`, `AccessToken`, `Page<T>`, telemetry name constants.

**Dependency budget:** BCL only. No `Microsoft.Extensions.*`, no third-party. This is the package SDK authors and domain assemblies reference so they can *consume* an `IApiClient` without pulling the framework.

**Rule:** if a type is needed to *call* an API client or *interpret its result*, it lives here. If it's needed to *build/configure* one, it lives in the core.

## Koras.ApiClient (core)

**Contains:** `KorasApiClient`, fluent builder + verb/pagination extensions, content builders, auth handlers and token providers, response caching handler, error factory, diagnostics (ActivitySource/Meter/logging), DI registration + options + validation.

**Dependencies (all Microsoft, MIT):**

| Package | Why | Public-API leakage |
|---|---|---|
| Microsoft.Extensions.Http | HttpClientFactory integration | `IHttpClientBuilder` exposed on `IKorasApiClientBuilder` (intentional escape hatch) |
| Microsoft.Extensions.Http.Resilience | standard retry/CB/timeout pipeline | `HttpStandardResilienceOptions` in `ConfigureResilience` (intentional) |
| Microsoft.Extensions.Caching.Memory | response cache store | none (handler internal) |
| Microsoft.Extensions.Options.ConfigurationExtensions | `IConfiguration` binding for options | `IConfiguration` in one overload (intentional) |

## Koras.ApiClient.Testing

**Contains:** `MockApiHandler`, `MockRequestSetup`, `RecordedRequest`, `KorasApiClientTestFactory`.

**Depends on:** core (to construct real clients over fake transports). Never referenced by shipping application code — test projects only.

## Future sibling packages

`Koras.ApiClient.Azure` (TokenCredential adapter), `Koras.ApiClient.HealthChecks`. Each depends on `Abstractions` (+ its own provider SDK), never on each other.

## Dependency direction (enforced by architecture tests)

```
Testing ──► Core ──► Abstractions ──► (BCL only)
```

- Abstractions references nothing in the solution.
- Core never references Testing.
- Samples/benchmarks may reference anything; nothing references them.
