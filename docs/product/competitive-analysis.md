# Competitive Analysis

## Existing NuGet alternatives

| Package | Strengths | Gaps Koras.ApiClient fills |
|---|---|---|
| **Refit** | Declarative interface-based clients, huge adoption | No built-in auth flows, resilience, pagination, or normalized error model; attribute magic hard to debug |
| **RestSharp** | Long history, simple API | Predates `HttpClientFactory` idioms; resilience/telemetry are DIY; error handling is status-code based |
| **Flurl.Http** | Excellent fluent URL/request builder | Testing story is global/static; no OAuth client-credentials flow, pagination, or typed error model |
| **Polly / Microsoft.Extensions.Http.Resilience** | Best-in-class resilience primitives | Not a client framework — no builder, auth, pagination, errors (Koras composes it, not competes) |
| **Microsoft Kiota** | OpenAPI-first generated clients | Requires OpenAPI descriptions & generation step; heavier model; not a hand-written-integration toolkit |
| **Raw HttpClientFactory + typed clients** | Zero dependencies | Every cross-cutting concern is manual; this is exactly the status quo pain |

## Differentiators

1. **Composition, not reinvention** — built on `HttpClientFactory` + `Microsoft.Extensions.Http.Resilience`; you can always drop to the raw `IHttpClientBuilder`.
2. **Normalized error model** — every outcome (transport failure, timeout, 4xx/5xx, RFC 9457 ProblemDetails, deserialization failure) becomes a typed `ApiError` with a category; no exception/null/status-code roulette.
3. **Auth flows included** — API key, static/delegated bearer, and cached OAuth2 client credentials with single-flight refresh, out of the box.
4. **Pagination as `IAsyncEnumerable<T>`** — cursor and page-number strategies with bounded loops and cancellation.
5. **First-class testing package** — `MockApiHandler` + `KorasApiClientTestFactory` so consumers never mock `HttpMessageHandler` by hand.
6. **Observability by default** — `ActivitySource`, `Meter`, and structured logs with redaction, ready for OpenTelemetry with one `AddSource`/`AddMeter` call.

## What could prevent adoption

- Refit's mindshare for simple cases → mitigate with a 5-minute quick start and a Refit migration guide
- Fear of another abstraction layer → mitigate with escape hatches and a "thin over HttpClient" architecture doc
- Trust in a new package → mitigate with tests, deterministic builds, Source Link, signed releases, semantic versioning, and public roadmap

## What we deliberately do not compete on

OpenAPI generation (Kiota/NSwag), non-HTTP protocols, and UI/desktop-specific scenarios.
