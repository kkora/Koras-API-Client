# Backward Compatibility Strategy

## Principles

1. Public surface is minimal by default — you can always widen, never narrow.
2. Additive evolution: new optional parameters via overloads (never defaults-changed), new members on options classes, new enum values only on enums documented as open (`ApiErrorCategory`; consumers must handle unknown values).
3. Behavioral compatibility counts: defaults (timeouts, retry counts, header names) only change in majors and are release-noted.

## Enforcement mechanisms

| Mechanism | Stage | Status |
|---|---|---|
| `EnablePackageValidation` | `dotnet pack` — validates TFM/API consistency; with `PackageValidationBaselineVersion` after first stable, validates against the previous release | On now; baseline wired at 1.0.0 |
| Public API baseline files (`PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt` via Microsoft.CodeAnalysis.PublicApiAnalyzers) | Build — any surface change requires an explicit file edit, visible in diff review | Adopted at 1.0.0 when the surface freezes; strategy: empty `Shipped` until 1.0, all preview surface in `Unshipped`, moved to `Shipped` at release |
| Architecture tests | CI — dependency direction, namespace rules | On now |
| SemVer + release checklist | Process — see docs/release/versioning.md | On now |

## Breaking-change process

1. Open an issue labeled `breaking-change` with migration notes.
2. Deprecate first when feasible: `[Obsolete("…, see https://…")]` for ≥1 minor release.
3. Ship in the next major with a `docs/migration/` entry and CHANGELOG "Breaking" section.
