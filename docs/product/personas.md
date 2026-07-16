# User Personas

## Persona 1 — Priya, senior backend developer (microservices)

- Builds ASP.NET Core services calling 6+ internal and external APIs
- Wants: one registration pattern, sane resilience defaults, traces in Grafana/Jaeger
- Fears: another abstraction that hides `HttpClient` when she needs low-level control
- Koras answer: everything is `HttpClientFactory` underneath; escape hatches (`WithContent`, `ConfigureResilience`, raw `IHttpClientBuilder`) are first-class

## Persona 2 — Marcus, SDK maintainer

- Ships a NuGet SDK wrapping his company's public API
- Wants: a stable foundation with a small dependency graph, strong-naming not required, MIT license, testable without network
- Fears: transitive dependency conflicts for his consumers
- Koras answer: `Abstractions` package has zero third-party dependencies; core depends only on `Microsoft.Extensions.*`

## Persona 3 — Aisha, platform engineer

- Defines golden paths for 40 development teams
- Wants: enforceable defaults (bounded retries, redacted logs, correlation IDs), configuration via `appsettings.json`, health/metrics dashboards per dependency
- Koras answer: options pattern with named clients, startup validation, built-in `Meter` and `ActivitySource`

## Persona 4 — Tom, junior developer

- First integration project; copies examples
- Wants: copy-paste quick start that is also production-correct
- Koras answer: the 5-minute quick start already includes resilience, telemetry, and typed errors — the easy path is the safe path
