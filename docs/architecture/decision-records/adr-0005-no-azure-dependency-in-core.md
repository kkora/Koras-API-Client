# ADR-0005: Managed identity via IAccessTokenProvider seam; Azure SDK stays out of core

**Status:** Accepted · 2026-07-16

## Context

"Managed identity integration" is a required feature, but referencing `Azure.Identity` from the core would impose a large, fast-moving dependency graph on every consumer, including non-Azure ones.

## Decision

The core ships the **seam**: `IAccessTokenProvider` + `WithBearerToken<TProvider>()`. Managed identity works today with a 5-line adapter (documented in `docs/features/authentication.md`); the official adapter ships later as `Koras.ApiClient.Azure` (0.5.0) referencing `Azure.Core` only.

## Consequences

- Core dependency graph stays Microsoft.Extensions-only.
- Azure users write (or later install) a tiny adapter; the guide includes copy-paste code.
- Same pattern will serve AWS/GCP credential sources without core changes.
