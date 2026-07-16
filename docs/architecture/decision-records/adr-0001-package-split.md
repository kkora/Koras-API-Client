# ADR-0001: Three-package split (Abstractions / Core / Testing)

**Status:** Accepted · 2026-07-16

## Context

The framework could ship as one package, a metapackage over many micro-packages, or a small set of focused packages. SDK authors need to expose `IApiClient` results without forcing the whole framework (and its `Microsoft.Extensions.*` graph) on their consumers, and application test projects need doubles without hand-rolling `HttpMessageHandler` fakes.

## Decision

Ship three packages: `Koras.ApiClient.Abstractions` (contracts/models, BCL-only), `Koras.ApiClient` (framework), `Koras.ApiClient.Testing` (test doubles). Provider integrations (Azure, HealthChecks) become future sibling packages against Abstractions.

## Alternatives considered

- **Single package** — simplest, but forces `Microsoft.Extensions.Http.Resilience` onto every assembly that only handles an `ApiResponse`; blocks future provider packages from staying lean.
- **Micro-packages per feature** (auth/pagination/caching separate) — maximizes modularity but multiplies versioning, docs, and consumer confusion for features that share internals; rejected as premature.

## Consequences

- Dependency direction `Testing → Core → Abstractions` enforced by architecture tests.
- Abstractions API changes are the most expensive — reviewed strictest.
- Consumers installing only `Koras.ApiClient` get Abstractions transitively; no metapackage needed.
