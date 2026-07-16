# Changelog

All notable changes to this project are documented in this file. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `Koras.ApiClient.Abstractions`: `IApiClient`, `IApiClientFactory`, `ApiRequest`, `ApiResponse`/`ApiResponse<T>`, `ApiError` + `ApiErrorCategory`, RFC 9457 `ProblemDocument`, `ApiClientException`, `IAccessTokenProvider`/`AccessToken`, `Page<T>`, `KorasApiClientTelemetry` constants.
- `Koras.ApiClient`: fluent request builder with verb extensions (`Get`/`Post`/`Put`/`Patch`/`Delete`/`Head`), JSON / form / multipart content, streaming download with progress, per-request timeout/correlation/cache controls.
- Normalized error model: transport, timeout, HTTP-status, ProblemDetails, and deserialization failures all map to categorized `ApiError`s; `Retry-After` surfaced; bounded error-body capture.
- Authentication: API key (header/query), bearer tokens via `IAccessTokenProvider` (single 401 refresh-retry), OAuth 2.0 client-credentials provider with expiry-skewed caching and single-flight refresh.
- Resilience: `Microsoft.Extensions.Http.Resilience` standard pipeline enabled by default; `ConfigureResilience` tuning hook.
- Pagination: `PaginateAsync` (cursor) and `PaginateByPageNumberAsync` as `IAsyncEnumerable<T>` with `maxPages` guards.
- Opt-in in-memory GET response caching with TTL and entry-size limits.
- Correlation IDs (configurable header, auto-generated, echoed on responses).
- Observability: `ActivitySource`/`Meter` (`Koras.ApiClient`), structured redacted logging.
- Dependency injection: `AddKorasApiClient` (default/named/config-bound), typed clients, `IApiClientFactory`, startup options validation.
- `Koras.ApiClient.Testing`: `MockApiHandler`, `RecordedRequest`, `KorasApiClientTestFactory`.

## [0.1.0-preview.1] — not yet released

Initial public preview (contents above will move here on release).
