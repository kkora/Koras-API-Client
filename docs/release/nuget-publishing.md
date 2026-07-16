# NuGet Publishing

## Trusted Publishing (OIDC) — no long-lived API key

Releases publish via **NuGet.org Trusted Publishing**: the `release.yml` workflow presents a GitHub OIDC token, and the `NuGet/login@v1` action exchanges it for a **short-lived** API key used only for that run. There is no `NUGET_API_KEY` repository secret to create, rotate, or leak.

### One-time setup on NuGet.org

1. Sign in to nuget.org as the owning profile (`kkora`).
2. Account → **Trusted Publishing** → add a policy:
   - Repository owner: `kkora`
   - Repository: `Koras-API-Client`
   - Workflow file: `release.yml`
   - Environment: `nuget-release`
3. Ensure the policy covers the `Koras.ApiClient*` package IDs (including first-time publishes of new IDs).

### One-time setup on GitHub

- *Settings → Environments → `nuget-release`*: add required reviewers if you want a manual approval gate before publishing (recommended).
- The workflow already declares `permissions: id-token: write`, which OIDC requires.

### How the workflow authenticates

```yaml
- uses: NuGet/login@v1
  id: nuget-login
  with:
    user: kkora          # NuGet.org profile owning the policy
- run: dotnet nuget push "artifacts/packages/*.nupkg"
       --api-key "${{ steps.nuget-login.outputs.NUGET_API_KEY }}"
       --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## What every package carries (wired in `src/Directory.Build.props` + per-project `.csproj`)

PackageId, version (from `Directory.Build.props` or `-p:Version` on tag builds), authors/company, description, tags, MIT license expression, project/repository URLs, package README, icon, release-notes link, XML docs, `.snupkg` symbols, Source Link, deterministic + CI build flags, `EnablePackageValidation`.

## Manual publish (break-glass only)

Trusted Publishing only works from the configured workflow. For an emergency manual push, create a **temporary, scoped, short-expiry** API key on nuget.org, use it once, then delete it:

```bash
dotnet pack --configuration Release --output artifacts/packages -p:Version=0.1.0-preview.1
dotnet nuget push "artifacts/packages/*.nupkg" --api-key "<temporary-key>" \
  --source https://api.nuget.org/v3/index.json --skip-duplicate
```

Prefer the tag-driven workflow — it guarantees tests ran against the exact published bits and needs no key handling at all.

## Post-publish verification

- Gallery pages render README + icon for all three packages
- `dotnet add package Koras.ApiClient --prerelease` into a scratch app: builds, quick start runs
- Symbols resolve (SourceLink step-through from a consuming app)

## Unlisting / yanking

Broken release? **Unlist** on NuGet.org (never delete), publish a fixed patch, note both in the CHANGELOG.
