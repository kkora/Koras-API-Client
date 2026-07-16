# ADR-0002: Target net8.0 / net9.0 / net10.0, no netstandard2.0

**Status:** Accepted · 2026-07-16

## Context

The prompt allows netstandard2.0 "only when broad legacy compatibility is genuinely needed". The framework's foundations — `Microsoft.Extensions.Http.Resilience`, keyed DI services, `TimeProvider`, `HttpRequestMessage.Options`, `IAsyncEnumerable` pagination ergonomics — are designed for modern .NET.

## Decision

Multi-target **net8.0;net9.0;net10.0** for all shipping packages. No netstandard2.0.

## Rationale

- net8.0 is the active LTS floor; net10.0 is the current LTS. This spans every supported modern runtime.
- netstandard2.0 would force polyfills, `#if` forests, and a degraded API (no default interface members, no `HttpRequestOptions`), for an audience (`.NET Framework 4.x`) that is not a target user of a new resilience-first client framework.
- Per-TFM builds let the compiler pick up runtime improvements (e.g., net9+ `SearchValues`-based paths in STJ) for free.

## Consequences

- .NET Framework consumers are unsupported; documented in README.
- When net8.0 leaves support (Nov 2026), a minor release may drop it per the support policy in `docs/release/versioning.md`.
