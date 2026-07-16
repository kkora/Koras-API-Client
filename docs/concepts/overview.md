# Concepts Overview

Five ideas explain the whole framework:

1. **Requests are data.** The fluent builder produces an `ApiRequest` (pure data); `IApiClient.SendAsync` executes it. Nothing dials out before a terminal call.
2. **Outcomes are data.** `ApiResponse`/`ApiResponse<T>` carry either a value or a categorized `ApiError` — network failures, timeouts, HTTP errors, and bad JSON all land in one taxonomy. Only caller cancellation and programmer errors throw. ([error-model](../architecture/error-model.md))
3. **Everything rides HttpClientFactory.** Auth, caching, and resilience are `DelegatingHandler`s in a pipeline (caching → resilience → auth → transport); the raw `IHttpClientBuilder` remains your escape hatch. ([architecture](../architecture/overview.md))
4. **Safe by default.** Registration alone gives you bounded retries + circuit breaker + timeouts, correlation IDs, redacted structured logs, and OTel-ready spans/metrics.
5. **Extensible at seams, not forks.** Token providers, handlers, resilience options, serializer options, pagination delegates. ([extension-model](../architecture/extension-model.md))

## Lifecycle, threading, cancellation — the contract

- Clients: thread-safe, cheap, factory-managed. Builders: single-use, not thread-safe.
- Every async API takes a `CancellationToken`; caller cancellation ⇒ `OperationCanceledException`, timeouts ⇒ `Timeout`-category errors ([error-handling](../features/error-handling.md)).
- Providers and handlers are singletons per named client and must be thread-safe.

Deep dives: [core abstractions](../api/public-api-design.md) · [request lifecycle diagrams](../architecture/diagrams.md) · feature guides in [docs/features](../features/).
