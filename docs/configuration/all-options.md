# Configuration Reference — all options

## `KorasApiClientOptions` (per named client)

| Option | Type | Default | Notes |
|---|---|---|---|
| `BaseAddress` | `Uri?` | null | absolute; trailing `/` recommended; relative request paths resolve against it |
| `Timeout` | `TimeSpan` | 30 s | overall `HttpClient` budget; align resilience timeouts for long ops |
| `UserAgent` | `string?` | `Koras.ApiClient/<ver>` | null ⇒ no default UA header |
| `DefaultHeaders` | dictionary | empty | attached to every request; per-request headers win |
| `EnableCorrelationId` | `bool` | true | auto-generate + attach correlation header |
| `CorrelationHeaderName` | `string` | `X-Correlation-ID` | |
| `JsonSerializerOptions` | `JsonSerializerOptions` | Web defaults | camelCase, case-insensitive; code-only (not config-bindable) |
| `CaptureErrorResponseBody` | `bool` | true | onto `ApiError.ResponseBody` |
| `MaxErrorResponseBodyLength` | `int` | 4096 | chars; 0 disables |
| `Caching.Enabled` | `bool` | false | client-wide GET caching (`WithResponseCaching` sets true) |
| `Caching.DefaultDuration` | `TimeSpan` | 60 s | when enabled and no per-request duration |
| `Caching.MaxEntrySizeBytes` | `long` | 1 MiB | bigger bodies bypass the cache |

## `ApiKeyAuthenticationOptions` (`WithApiKey`)

`ApiKey` (required), `HeaderName` = `X-Api-Key`, `Location` = `Header`|`QueryString`, `QueryParameterName` = `api_key`.

## `ClientCredentialsOptions` (`WithClientCredentials`)

`TokenEndpoint`/`ClientId`/`ClientSecret` (required), `Scopes` (list → space-joined `scope`), `AdditionalParameters` (extra form fields, e.g. `audience`), `RefreshSkew` = 60 s.

## Resilience (`ConfigureResilience`)

Exposes `HttpStandardResilienceOptions` directly: `RateLimiter`, `TotalRequestTimeout` (30 s), `Retry` (3 attempts, exponential+jitter, honors Retry-After), `CircuitBreaker`, `AttemptTimeout` (10 s). See [features/resilience.md](../features/resilience.md).

## Binding from configuration

Everything except `JsonSerializerOptions` binds from a section via `AddKorasApiClient(name, configuration.GetSection(...))` — examples in [appsettings.md](appsettings.md); validation rules in [validation.md](validation.md).
