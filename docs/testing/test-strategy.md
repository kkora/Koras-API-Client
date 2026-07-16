# Test Strategy

## Principles

1. **Test through the real pipeline.** Unit tests use `Koras.ApiClient.Testing`'s `MockApiHandler` under a real `KorasApiClient` (dogfooding, ADR-0006) — serialization, URI composition, correlation, and error normalization are always exercised, never stubbed.
2. **Every feature ships with:** happy path, invalid input, failure path, cancellation, and thread-safety tests where shared state exists (definition of done).
3. **Behavior coverage over percentage.** The error-mapping table, timeout-vs-cancellation distinction, 401-replay rules, cache-key semantics, and pagination bounds each have dedicated tests because those are the bug factories.

## Test categories → where they live

| Category | Project / file |
|---|---|
| Unit (builder, errors, auth, caching, DI, pagination, telemetry, download/multipart) | `tests/Koras.ApiClient.UnitTests` |
| Integration (real Kestrel: retries, ProblemDetails, OAuth E2E, streaming) | `tests/Koras.ApiClient.IntegrationTests` |
| Architecture (dependency direction, namespaces, CancellationToken hygiene) | `UnitTests/ArchitectureTests.cs` |
| Serialization (web defaults, string/204/malformed bodies) | `ErrorMappingTests`, `RequestBuilderTests` |
| Concurrency (single-flight token refresh) | `ClientCredentialsTests` |
| Cancellation / timeout / retry / error mapping / DI / config validation | dedicated tests per feature file |
| Security-focused (log redaction, bounded capture) | `TelemetryTests`, `ErrorMappingTests` |
| API compatibility | `EnablePackageValidation` at pack; baselines at 1.0 |
| Package consumption | consume `.nupkg` in a scratch project per release checklist |
| Performance regression | `benchmarks/` (BenchmarkDotNet, run on demand) |

## Frameworks & tools

xUnit + standard asserts (no assertion-library license risk), coverlet for coverage, in-process Kestrel for integration (no Testcontainers needed — no external infrastructure), reflection-based architecture tests (no extra dependency).

## Matrix

Tests execute on **net8.0 and net10.0** (oldest and newest supported runtimes) on every CI run, on Ubuntu and Windows (`test.yml`).

## Coverage target

Practical target: ≥85 % line / ≥80 % branch on `Koras.ApiClient` + `Abstractions`, with the error-mapping and URI-composition branches at 100 %. Collected via `--collect:"XPlat Code Coverage"` in CI; numbers are advisory, the mandatory bar is the behavior list above.
