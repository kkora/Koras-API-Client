# Dependency & Supply-Chain Security

## Dependency posture

Runtime graph is Microsoft-only: `Microsoft.Extensions.Http`, `Microsoft.Extensions.Http.Resilience` (brings Polly v8 — .NET Foundation, BSD-3), `Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.Options.ConfigurationExtensions`. `Abstractions` has **zero** package references. Rules and the justification template for any addition: [architecture/dependency-rules.md](../architecture/dependency-rules.md).

## Controls in CI

| Control | Where |
|---|---|
| Central version pinning (no floats) | `Directory.Packages.props` |
| Vulnerability audit (fails build) | `build.yml`: `dotnet list package --vulnerable --include-transitive` |
| Dependency review on PRs (fails on ≥moderate, denies GPL/AGPL) | `dependency-review.yml` |
| CodeQL (security-and-quality suite, weekly + per PR) | `codeql.yml` |
| Automated update PRs | `dependabot.yml` (NuGet + Actions, weekly, grouped) |
| Deterministic builds + Source Link + snupkg symbols | `Directory.Build.props` — consumers can verify bits ↔ source |
| NuGet source pinned to nuget.org with signature validation | `NuGet.Config` |

## Secret scanning

Enable GitHub secret scanning + push protection on the repository (Settings → Code security). The `.gitignore` excludes `secrets.json`, `*.local.env`, and local appsettings variants as a second line of defense.

## SBOM

Generate on release when required by consumers:

```bash
dotnet tool install --global Microsoft.Sbom.DotNetTool
sbom-tool generate -b artifacts/packages -bc . -pn Koras.ApiClient -pv <version> -ps "Koras Technologies"
```

Attach the SBOM to the GitHub release alongside the packages.

## Package signing

Packages publish to NuGet.org via a scoped API key held as an environment-protected repository secret (`NUGET_API_KEY`); NuGet.org repository-signs all packages. Author signing (NuGet `sign`) is planned for 1.0 once an org code-signing certificate is provisioned — tracked in the release checklist.
