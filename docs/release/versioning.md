# Versioning Policy

Semantic Versioning 2.0 across all packages; the three packages version **in lockstep** (one release train).

| Release type | Contains | Examples |
|---|---|---|
| **Patch** (x.y.Z) | bug fixes, doc/metadata fixes, dependency patch bumps — no API or behavior-contract changes | fixing a wrong error category |
| **Minor** (x.Y.0) | additive API (new methods/options/overloads), new features, dependency minor bumps, obsoletions | new auth scheme |
| **Major** (X.0.0) | breaking API or **behavioral** changes (defaults: timeouts, retry counts, header names), TFM drops, obsolete removals | changing the default correlation header |
| **Prerelease** | `-preview.N` (API may still change), `-rc.N` (frozen, stabilization only) | `0.1.0-preview.1` |

## Pre-1.0 caveat

While the major version is 0, minor releases may include breaking changes (SemVer §4) — each one is CHANGELOG-flagged with migration notes.

## Deprecation process

`[Obsolete("why + link")]` for at least one minor release before removal in the next major; obsoletions are listed in the CHANGELOG under "Deprecated".

## Supported frameworks & support window

Targets track supported .NET: currently `net8.0;net9.0;net10.0`. A TFM is dropped (major release) no earlier than its Microsoft end-of-support date. Security fixes go to the latest release of the current major and the final minor of the previous major for 12 months after a new major ships.

## Dependency updates

Patch bumps of `Microsoft.Extensions.*` ride patch releases; minor/major dependency bumps ride minor releases with compatibility notes.
