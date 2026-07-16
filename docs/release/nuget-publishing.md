# NuGet Publishing

## Required secrets & environment

| Item | Where | Notes |
|---|---|---|
| `NUGET_API_KEY` | GitHub repo secret, scoped to the `nuget-release` **environment** | NuGet.org key scoped to push `Koras.ApiClient*` only; rotate every 90 days; never in source |
| `nuget-release` environment | GitHub Settings → Environments | require reviewer approval before the publish job runs |

## What every package carries (wired in `src/Directory.Build.props` + per-project `.csproj`)

PackageId, version (from `Directory.Build.props` or `-p:Version` on tag builds), authors/company, description, tags, MIT license expression, project/repository URLs, package README, icon, release-notes link, XML docs, `.snupkg` symbols, Source Link, deterministic + CI build flags, `EnablePackageValidation`.

## Manual publish (break-glass only)

```bash
dotnet pack --configuration Release --output artifacts/packages -p:Version=0.1.0-preview.1
dotnet nuget push "artifacts/packages/*.nupkg" --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json --skip-duplicate
```

Prefer the tag-driven workflow — it guarantees tests ran against the exact published bits.

## Post-publish verification

- Gallery pages render README + icon for all three packages
- `dotnet add package Koras.ApiClient --prerelease` into a scratch app: builds, quick start runs
- Symbols resolve (SourceLink step-through from a consuming app)

## Unlisting / yanking

Broken release? **Unlist** on NuGet.org (never delete), publish a fixed patch, note both in the CHANGELOG.
