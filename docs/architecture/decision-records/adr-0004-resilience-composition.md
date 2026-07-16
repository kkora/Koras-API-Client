# ADR-0004: Compose Microsoft.Extensions.Http.Resilience, don't reimplement

**Status:** Accepted · 2026-07-16

## Context

Retry/circuit-breaker/timeout could be hand-built (full control, no deps) or delegated to `Microsoft.Extensions.Http.Resilience` (Polly v8 based, Microsoft-maintained, telemetry-integrated).

## Decision

`AddKorasApiClient` attaches the **standard resilience handler by default** (rate-limiter, total timeout, retry with jitter honoring `Retry-After`, circuit breaker, attempt timeout). Tuning is exposed via `IKorasApiClientBuilder.ConfigureResilience(Action<HttpStandardResilienceOptions>)`. We intentionally expose the `HttpStandardResilienceOptions` type rather than wrapping it: wrapping would lag upstream features and double the docs surface.

## Consequences

- Polly v8 arrives transitively — documented in dependency rules; never referenced directly by our code.
- Resilience is default-on ("the easy path is the safe path"); opting out means using the raw `IHttpClientBuilder` seam, documented for advanced scenarios.
- Our integration tests cover retry behavior end-to-end but we do not re-test Polly internals.
