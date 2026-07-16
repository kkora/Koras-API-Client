# Implementation Plan

## Milestones

| # | Milestone | Content | Exit criteria |
|---|---|---|---|
| 0 | Repository foundation | solution, Directory.Build/Packages.props, global.json, editorconfig, community files, CLAUDE.md, CI skeleton | `dotnet build` empty solution green |
| 1 | Abstractions | all contract types + XML docs | compiles warning-free on 3 TFMs |
| 2 | Core client | `KorasApiClient`, URI composition, error factory, telemetry, fluent builder, content builders, download | unit tests for builder + error mapping green |
| 3 | DI & configuration | registration, `IKorasApiClientBuilder`, options validation, config binding, typed clients, factory | DI unit tests green; startup validation proven |
| 4 | Auth & handler pipeline | API key, bearer, client credentials, response caching handler | auth + caching unit tests green |
| 5 | Pagination & resilience integration | paginate extensions; standard resilience wiring | integration tests (retry, pagination) green |
| 6 | Observability | LoggerMessage logs, Activity tags, metrics | telemetry unit tests green |
| 7 | Testing package + samples | MockApiHandler, test factory; Console/MinimalApi/Worker samples | samples build & run |
| 8 | Hardening | security review, benchmarks, coverage review | `dotnet list package --vulnerable` clean; benchmarks compile |
| 9 | Packaging & release readiness | pack validation, README-in-package, icon, workflows finalized, release docs | `dotnet pack` produces validated .nupkg + .snupkg |

## First five implementation tasks

1. T-000: Solution + build props + CPM + analyzers (Milestone 0)
2. T-101: Error/response model types in Abstractions (Milestone 1)
3. T-102: `IApiClient`/`IApiClientFactory`/token/pagination contracts (Milestone 1)
4. T-201: `KorasApiClient.SendAsync` with URI composition, correlation, telemetry, error normalization (Milestone 2)
5. T-202: `ApiRequestBuilder` + verb extensions + content builders (Milestone 2)

## Highest risks → mitigations

See docs/planning/risks.md.

## Recommended MVP

Packages `Abstractions` + `Koras.ApiClient` + `Testing` with feature set per docs/features/mvp-scope.md.

## Deferred

Azure adapter, health-check package, idempotency keys, signing, distributed cache, OpenAPI interop, offline queue (docs/features/future-roadmap.md).
