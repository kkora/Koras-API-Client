# Product Roadmap

## 0.1.0-preview.1 (MVP preview)

- `Koras.ApiClient.Abstractions`, `Koras.ApiClient`, `Koras.ApiClient.Testing`
- Fluent request builder, JSON + multipart + form content, upload/download streaming
- API key, bearer token, OAuth2 client credentials authentication
- Standard resilience on by default (retry, circuit breaker, timeout, rate-limit awareness via `Retry-After`)
- Normalized `ApiError` model with RFC 9457 ProblemDetails support
- Pagination (`cursor` and `page-number`) as `IAsyncEnumerable<T>`
- Correlation IDs, structured logging, `ActivitySource` + `Meter`
- Response caching (opt-in, memory-backed)
- DI registration with named + typed clients, options validation

## 0.1.0

- API stabilization from preview feedback; PublicAPI baseline files committed

## 0.5.0

- `Koras.ApiClient.Azure` (managed identity / `TokenCredential` adapter package)
- Idempotency-key support; request-signing extension point
- Health-check integration package
- Distributed (`IDistributedCache`) response caching

## 1.0.0

- Frozen public API with compatibility gates in CI (package validation + API baselines)
- Performance-tuned hot paths with published benchmarks
- Complete documentation site

## 2.0.0 candidates / experimental

- OpenAPI-driven client generation interop
- Offline request queue with durable storage
- Bulkhead isolation policies per named client
- Contract-testing helpers (provider stubs from recorded traffic)

## Open-source & community strategy

- MIT license, public GitHub repo (https://github.com/KKORA/koras-api-client)
- `CONTRIBUTING.md` + good-first-issue labels; ADRs public so design rationale is discoverable
- Semantic versioning with documented breaking-change and deprecation process (`docs/release/versioning.md`)

## Monetization

None planned. The package is a free OSS foundation; Koras Technologies benefits via consulting credibility and shared maintenance.

## Naming assessment

- **Package ID `Koras.ApiClient`** — available-style org-prefixed ID, clear category, no trademark conflicts found with common packages
- **Root namespace `Koras.ApiClient`** — under the mandated `Koras` root; no BCL collisions (`ApiClient` is not a BCL namespace)
- Sub-packages follow NuGet convention: `Koras.ApiClient.Abstractions`, `Koras.ApiClient.Testing`
