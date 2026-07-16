# Milestones

| Milestone | Name | Scope | Validation gate |
|---|---|---|---|
| 0 | Repository foundation | build infra, community files, CI skeleton | `dotnet build` green |
| 1 | Abstractions & core models | contract types | 3-TFM warning-free build |
| 2 | Core implementation | client, builder, errors, content, download | unit tests green |
| 3 | DI & configuration | registration, options, validation, typed clients | DI tests green |
| 4 | Providers/handlers | auth handlers, token providers, caching handler | auth/caching tests green |
| 5 | Integration behaviors | pagination, resilience | in-process Kestrel integration tests green |
| 6 | Observability | logs/traces/metrics + redaction | telemetry tests green |
| 7 | Samples & documentation | 3 samples, feature guides, getting started | samples run; docs complete |
| 8 | Performance & security hardening | benchmarks, threat model, audits | audit clean; benchmarks compile |
| 9 | NuGet packaging & release | pack, validate, workflows, release docs | validated .nupkg/.snupkg artifacts |

Milestones 0–9 all land in 0.1.0-preview.1; the release plan beyond that is in docs/release/release-process.md.
