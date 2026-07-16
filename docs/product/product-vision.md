# Product Vision — Koras.ApiClient

## Vision statement

**Koras.ApiClient** is a resilient, observable, secure-by-default HTTP API client framework for enterprise .NET applications. It gives teams a single, consistent way to call HTTP APIs — with authentication, retries, circuit breaking, pagination, caching, correlation, telemetry, and standardized errors built in — so they stop rebuilding the same `HttpClient` plumbing in every service.

## Why this package should exist

Every .NET team that integrates with more than one HTTP API ends up writing the same infrastructure:

- Delegating handlers for API keys, bearer tokens, and OAuth client credentials
- Retry/timeout/circuit-breaker policies, copy-pasted between services with drift
- Pagination loops (page-number, offset, cursor) re-implemented per API
- Ad-hoc error handling: some code throws, some returns `null`, some inspects `StatusCode`
- Logging and tracing bolted on inconsistently
- Response caching hand-rolled with `IMemoryCache`

The BCL and `Microsoft.Extensions.Http.Resilience` provide excellent *primitives*, but no opinionated, cohesive *framework* that composes them. Koras.ApiClient is that composition layer: it stands **on** `HttpClientFactory`, `System.Text.Json`, and `Microsoft.Extensions.Http.Resilience` rather than replacing them.

## Guiding principles

1. **Secure by default** — resilience on by default, bounded retries, redacted logs, no secrets in URLs unless explicitly opted in, no secrets logged ever.
2. **Never fight the platform** — `HttpClientFactory`, `IOptions`, `ILogger`, `ActivitySource`, `Meter`, `TimeProvider`. No parallel universe.
3. **Errors are data** — `ApiResponse`/`ApiError` normalize every failure (network, timeout, 4xx/5xx, deserialization) into one typed model; exceptions are opt-in via `EnsureSuccess()`.
4. **Small, honest surface** — a fluent builder and a handful of interfaces. No magic, no reflection-heavy binding, no source-of-truth hidden in statics.
5. **Extensible at the seams** — token providers, message handlers, pagination selectors, and serializer options are all replaceable.

## What we will NOT build

- A code generator for OpenAPI (deferred; integrates rather than competes with existing generators)
- A GraphQL / gRPC / SOAP client
- A service mesh or API gateway
- Response deserialization for non-JSON formats (XML etc.) in the core — extensible via `HttpContent`
- Provider-specific SDKs (GitHub, Stripe, …) — Koras.ApiClient is the toolkit SDK authors use

## Success criteria

- A developer makes their first authenticated, resilient, traced API call within **5 minutes** of `dotnet add package`.
- Zero mandatory configuration beyond a base address.
- A team can standardize all outbound HTTP across a microservice fleet on this package without losing access to any underlying `HttpClient` capability.
