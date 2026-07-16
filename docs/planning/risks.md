# Risk Register

| # | Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| R1 | **API churn after release** — fluent surface locked in too early | Med | High | 0.1.0-preview cycle; PublicAPI baselines at 1.0; minimal-surface rule |
| R2 | **Timeout vs cancellation semantics wrong** — subtle `TaskCanceledException` handling differs across runtimes | Med | High | dedicated unit + integration tests for pre-cancelled, mid-flight cancel, per-request timeout, client timeout |
| R3 | **401-retry replays consumed content** | Med | High | retry only when `Content is null`; documented; tested |
| R4 | **Cache poisoning / cross-tenant cache leakage** | Low | High | opt-in caching only; key includes client name + full URI; docs warn on per-user endpoints; no caching of authenticated-variant by default guidance |
| R5 | **Secrets leak into logs/errors** | Med | High | path-only URL logging, header denylist, bounded body capture, security tests asserting absence |
| R6 | **Dependency drift** (Microsoft.Extensions majors, Polly transitives) | Med | Med | CPM single pin point; Dependabot; compat CI on 3 TFMs |
| R7 | **Unbounded pagination loops** on misbehaving APIs | Med | Med | `maxPages` default 1000, cancellation-first design |
| R8 | **Resilience defaults harm some workloads** (e.g., retrying non-idempotent POST) | Med | Med | standard handler's safe defaults; `ConfigureResilience` escape hatch; documented loudly |
| R9 | **Testing package drifts from real pipeline** | Low | Med | core test suite consumes the Testing package (dogfooding) |
| R10 | **Package trust barrier for adoption** | High | Med | deterministic builds, Source Link, symbols, CI badges, published threat model |
