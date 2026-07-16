# CLAUDE.md — Koras.ApiClient

## 1. Project mission

Koras.ApiClient is a resilient, observable, secure-by-default HTTP API client framework for enterprise .NET (see `docs/product/product-vision.md`). Three shipping packages: `Koras.ApiClient.Abstractions` (contracts, BCL-only), `Koras.ApiClient` (framework), `Koras.ApiClient.Testing` (consumer test doubles).

## 2. Package scope

In scope: fluent HTTP requests, auth (API key / bearer / client credentials), resilience composition, pagination, opt-in response caching, correlation, telemetry, normalized errors, DI. Out of scope: OpenAPI generation, non-HTTP protocols, provider SDKs (Azure adapter is a future sibling package). Do not add out-of-scope features without an ADR.

## 3. Architecture rules

- Dependency direction: `Testing → Core → Abstractions → BCL only`. Never reversed. Enforced by tests in `ArchitectureTests.cs`.
- All cross-cutting HTTP behavior is a `DelegatingHandler` on the `HttpClientFactory` pipeline — never bypass the factory.
- Errors are data (`ApiResponse`/`ApiError`); only caller cancellation and programmer errors throw from `SendAsync`.
- New extension points must use the existing seams (`IAccessTokenProvider`, handlers, `ConfigureResilience`, JSON options, pagination delegates) before inventing new ones. Major decisions get an ADR in `docs/architecture/decision-records/`.

## 4. Coding conventions

Modern C#: file-scoped namespaces, nullable enabled, `async`/`await` with `ConfigureAwait(false)` in library code, `CancellationToken` last parameter with default, pattern matching over type checks. No regions. Warnings are errors.

## 5. Naming conventions

See `docs/api/naming-guidelines.md`. Framework types `Koras*`-prefixed; payload types unprefixed; `With*` fluent config; `Add*` on IServiceCollection; tests `Method_Scenario_Expectation`.

## 6. Dependency restrictions

Abstractions: BCL only. Core: `Microsoft.Extensions.*` only. Testing: core + BCL, no test frameworks. All versions pinned in `Directory.Packages.props` (CPM). See `docs/architecture/dependency-rules.md`.

## 7. Public API rules

Minimal surface; everything not needed is `internal`. XML docs mandatory on public members. Apply `docs/api/public-api-review-checklist.md` on every surface change. No third-party types in signatures except approved seams (`IHttpClientBuilder`, `HttpStandardResilienceOptions`, `IConfiguration`).

## 8. Test requirements

Every feature: happy path, invalid input, failure path, cancellation, and thread-safety where shared state exists. Unit tests use `Koras.ApiClient.Testing` (dogfood). Integration tests use the in-process Kestrel server in `tests/Koras.ApiClient.IntegrationTests`. Never claim tests pass without running them.

## 9. Documentation requirements

Public member ⇒ XML docs. Feature change ⇒ update `docs/features/<feature>.md` + CHANGELOG `Unreleased`. New sample-visible behavior ⇒ update samples.

## 10. Security requirements

No secrets in logs, URLs (unless explicit query-key opt-in), exceptions, or error bodies. Bounded buffers (error body capture, cache entry size, pagination maxPages). Validate options at startup. See `docs/security/`.

## 11. Performance requirements

No sync-over-async (`.Result`, `.Wait()`, `Thread.Sleep`). Stream large payloads (`DownloadAsync`, multipart from streams). Avoid allocation in the send hot path where reasonable; benchmark changes to `KorasApiClient.SendAsync` with `benchmarks/`.

## 12. Git workflow

`main` is protected; work on feature branches `feature/<topic>`, `fix/<topic>`. Conventional-ish commit subjects (imperative, ≤72 chars). **Never add Claude as committer, author, or co-author; no `Co-Authored-By: Claude` trailers — commits are attributed to the human user only.**

## 13. Pull-request checklist

Build green on all TFMs · tests added and passing · public API checklist applied · docs + CHANGELOG updated · no new dependency without justification block · no secrets in diff.

## 14. Release workflow

Tag `v<version>` → `release.yml` builds, tests, packs, publishes to NuGet.org via **Trusted Publishing** (OIDC exchanged for a short-lived key by `NuGet/login`; no long-lived secret), gated by the `nuget-release` environment. See `docs/release/release-process.md`.

## 15. Definition of done

See `docs/planning/definition-of-done.md` — build, tests executed, docs, observability, security, API review, pack validation, no placeholders.

## 16. Commands Claude should use

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet pack --configuration Release --output artifacts/packages
dotnet format --verify-no-changes
dotnet list package --vulnerable --include-transitive
dotnet list package --outdated
```

## 17. Files Claude must not modify without justification

`LICENSE`, `Directory.Packages.props` version pins, `.github/workflows/release.yml`, `global.json`, ADRs (append new ones instead), `PublicAPI.Shipped.txt` (once introduced).

## 18. Rules for adding dependencies

Document in the PR: why, alternatives, license, maintenance status, security implications, size impact, public-API leakage. MIT/Apache-2.0 only. Pin centrally. Run vulnerability audit.

## 19. Rules for breaking changes

Follow `docs/api/backward-compatibility.md`: issue labeled `breaking-change`, obsolete-first when feasible, major version only, migration doc + CHANGELOG "Breaking" section.

## 20. Required validation before completing a task

`dotnet build --configuration Release` (zero warnings) → `dotnet test --configuration Release` (all green) → `dotnet pack` when packaging-affecting → update CHANGELOG/docs → confirm no secrets or placeholder code in the diff.
