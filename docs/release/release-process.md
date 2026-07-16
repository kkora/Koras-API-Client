# Release Process

## Flow

1. **Prepare** — CHANGELOG: move `Unreleased` into a version section with date; bump `VersionPrefix`/`VersionSuffix` in `Directory.Build.props`; run the [release checklist](release-checklist.md).
2. **PR** the release prep to `main`; CI must be fully green (build, tests ×2 TFM ×2 OS, pack validation, CodeQL, audit).
3. **Tag** — `git tag v0.1.0-preview.1 && git push origin v0.1.0-preview.1`.
4. **Automation** (`release.yml`, environment `nuget-release` with required reviewer): restore → build (`-p:Version` from tag) → test → pack (validated) → `dotnet nuget push` to NuGet.org (`--skip-duplicate`) → GitHub release with generated notes + `.nupkg`/`.snupkg` attached.
5. **Verify** — packages visible on NuGet.org, consumption smoke test (see [compatibility-testing](../testing/compatibility-testing.md)), README renders on the gallery page.
6. **Announce** — GitHub Discussions post; roadmap updated.

## Initial release plan

| Version | Gate |
|---|---|
| **0.1.0-preview.1** | this repository state: MVP features, 224 green test runs, validated packages |
| **0.1.0** | preview feedback triaged; no open correctness bugs; docs verified against final API |
| **0.5.0** | `Koras.ApiClient.Azure` + health-check package; PublicAPI files drafted |
| **1.0.0** | API frozen (PublicAPI.Shipped committed, `PackageValidationBaselineVersion` wired), benchmark baseline published, author signing in place |

## Hotfix path

Branch `release/x.y` from the tag → fix + test → tag `vx.y.z` → same automation. Forward-port the fix to `main` immediately.
