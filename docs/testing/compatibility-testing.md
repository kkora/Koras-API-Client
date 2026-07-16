# Compatibility Testing

## Target-framework compatibility

- Shipping packages multi-target `net8.0;net9.0;net10.0`; test projects run on **net8.0 and net10.0** (the boundary runtimes), on Ubuntu and Windows in CI.
- A regression that only reproduces on one TFM (e.g., serializer behavior differences) therefore fails CI.

## Package/API compatibility

- `EnablePackageValidation` runs at every `dotnet pack`: validates that all declared TFMs expose consistent API surfaces and that nothing required is missing from the package.
- After 1.0.0, `PackageValidationBaselineVersion` will compare each pack against the last stable release, failing on any binary/source breaking change.
- PublicAPI baseline files (Microsoft.CodeAnalysis.PublicApiAnalyzers) are adopted at the 1.0 surface freeze — strategy in [docs/api/backward-compatibility.md](../api/backward-compatibility.md).

## Consumer-consumption check (release checklist)

Before publishing, the packed `.nupkg` is installed into a scratch console project from a local feed:

```bash
dotnet pack -c Release -o artifacts/packages
dotnet new console -n PackageSmokeTest && cd PackageSmokeTest
dotnet nuget add source ../artifacts/packages -n local
dotnet add package Koras.ApiClient --prerelease
dotnet build && dotnet run   # quick-start snippet must compile and run
```

This catches packaging-only failures (missing README/icon, bad dependency ranges, XML docs absent) that project references never see.

## Dependency compatibility

Dependabot proposes `Microsoft.Extensions.*` bumps weekly; CI re-runs the full matrix on each bump, catching upstream behavioral changes early.
