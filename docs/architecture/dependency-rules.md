# Dependency Rules

## Adding a dependency to any shipping package requires

1. Justification in the PR description: why, alternatives considered, license, maintenance status, security posture, size, and whether it leaks into the public API.
2. It must be MIT/Apache-2.0 compatible.
3. Central version pin in `Directory.Packages.props` (CPM is mandatory; no versions in `.csproj`).
4. `dotnet list package --vulnerable --include-transitive` clean.

## Hard rules

- **Abstractions:** BCL only. Adding any PackageReference is a breaking architectural change requiring an ADR.
- **Core:** `Microsoft.Extensions.*` family only. Third-party (including Polly directly — it arrives transitively via the resilience package) is prohibited; use the extension seams instead.
- **Testing:** core + BCL. No test frameworks (must stay framework-agnostic for xUnit/NUnit/MSTest consumers).
- Provider packages (Azure, …) may add exactly their provider SDK.
- No floating versions (`*`), no prerelease dependencies in stable releases.

## Framework targeting

- Shipping packages: `net8.0;net9.0;net10.0`. `netstandard2.0` is intentionally **not** targeted: the package's foundations (HttpClientFactory resilience, keyed DI, `TimeProvider`) are net8+; legacy consumers are out of scope (ADR-0002).
- Tests: `net8.0;net10.0` (oldest + newest supported runtime).
- Dependency line: `Microsoft.Extensions.*` 9.x — runs on all three targets and keeps a single dependency set.

## Update policy

- Dependabot weekly for NuGet + GitHub Actions.
- Patch bumps auto-mergeable when CI is green; minor/major bumps require review notes on breaking-change risk.
