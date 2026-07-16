# Architecture Overview

## Package topology

Koras.ApiClient is a **core + abstractions + testing** trio (not a monolith, not a metapackage):

```
Koras.ApiClient.Abstractions   (contracts & models; zero third-party deps)
        ▲
Koras.ApiClient                (framework; Microsoft.Extensions.* only)
        ▲
Koras.ApiClient.Testing        (test doubles for consumers)
```

Provider-specific integrations (e.g. Azure managed identity) become sibling packages depending on `Abstractions`, keeping the core dependency graph small.

## Layering inside the core

```
┌──────────────────────────────────────────────────────────┐
│  DI layer: AddKorasApiClient / IKorasApiClientBuilder     │
├──────────────────────────────────────────────────────────┤
│  Fluent layer: verb extensions, ApiRequestBuilder,        │
│  MultipartBuilder, pagination extensions                  │
├──────────────────────────────────────────────────────────┤
│  Client: KorasApiClient (IApiClient) — URI composition,   │
│  correlation, telemetry, error normalization, (de)serial. │
├──────────────────────────────────────────────────────────┤
│  Handler pipeline (HttpClientFactory):                    │
│  auth handlers → response caching → standard resilience   │
├──────────────────────────────────────────────────────────┤
│  HttpClient / SocketsHttpHandler (BCL)                    │
└──────────────────────────────────────────────────────────┘
```

Key rule: **the client never bypasses `HttpClientFactory`.** All cross-cutting HTTP behavior is a `DelegatingHandler` so it composes with anything the consumer adds via the raw `IHttpClientBuilder`.

## Request lifecycle

1. Builder produces an immutable-ish `ApiRequest` (data only, no behavior).
2. `KorasApiClient.SendAsync` composes the URI, applies default + per-request headers, stamps a correlation ID, starts an `Activity`, and links per-request timeout with the caller's `CancellationToken`.
3. The `HttpRequestMessage` flows through the handler pipeline (auth → caching → resilience → transport).
4. The response (or transport exception) is normalized: success → `ApiResponse`/`ApiResponse<T>`; failure → categorized `ApiError` (ProblemDetails parsed when present). Metrics and logs are recorded in all paths.
5. Only caller cancellation escapes as an exception; every other outcome is data.

## Threading & lifetime model

- `KorasApiClient` is **thread-safe** and cheap; `IApiClientFactory` is a singleton; typed clients are transient wrappers.
- `ApiRequestBuilder` is **not** thread-safe (single logical request; build-and-send).
- Underlying `HttpClient` lifetime is owned by `HttpClientFactory` (handler rotation, DNS refresh — no sockets exhaustion).
- Token providers are singletons per named client with single-flight refresh.

## Async & cancellation model

- Async-only public surface; no sync-over-async anywhere; `ConfigureAwait(false)` throughout the library.
- Every public async method accepts a `CancellationToken`.
- Caller cancellation ⇒ `OperationCanceledException` (never swallowed). Timeout (per-request or client) ⇒ `ApiError` with `Category = Timeout`.

## Versioning & compatibility strategy

- SemVer 2.0. Public surface is deliberately minimal; anything not required is `internal`.
- `EnablePackageValidation` guards against accidental TFM/API drops at pack time; API baseline files gate 1.0+ (see docs/api/backward-compatibility.md).
- Options classes are additive-only; enums reserve `Unknown = 0`.
