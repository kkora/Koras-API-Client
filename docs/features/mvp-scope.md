# MVP Scope (0.1.0-preview.1)

## In scope

Three packages:

1. **Koras.ApiClient.Abstractions** — contracts and models only, zero third-party dependencies: `IApiClient`, `IApiClientFactory`, `ApiRequest`, `ApiResponse`/`ApiResponse<T>`, `ApiError`, `ApiErrorCategory`, `ProblemDocument`, `ApiClientException`, `IAccessTokenProvider`, `AccessToken`, `Page<T>`.
2. **Koras.ApiClient** — the framework: fluent builder, verb extensions, content helpers (JSON/form/multipart), streaming download, error normalization, auth handlers (API key, bearer, client credentials), default-on standard resilience, pagination, opt-in memory response caching, correlation IDs, logging/tracing/metrics, DI registration with named + typed clients and validated options.
3. **Koras.ApiClient.Testing** — `MockApiHandler`, `RecordedRequest`, `KorasApiClientTestFactory`.

Quality bar for MVP:

- Multi-target `net8.0;net9.0;net10.0`
- Warnings as errors, nullable enabled, XML docs on every public member
- Unit + integration + architecture tests green in CI
- Deterministic build, Source Link, symbols, package validation
- README quick start verified against the shipped samples

## Explicitly deferred (see future-roadmap.md)

Azure managed-identity adapter package, health-check package, idempotency keys, request signing, distributed caching, OpenAPI interop, offline queue, bulkhead presets.

## MVP acceptance criteria

1. `dotnet add package Koras.ApiClient` + 4 lines of DI = working resilient, traced, authenticated call.
2. All failure modes reachable in tests produce a categorized `ApiError` (never an unhandled framework exception, except caller cancellation).
3. A consumer can unit-test their integration code using only `Koras.ApiClient.Testing`.
4. `dotnet pack` produces validated packages with README, icon, symbols, and correct metadata.
