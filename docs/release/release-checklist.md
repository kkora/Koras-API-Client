# Release Checklist

## Code & tests

- [ ] `dotnet build -c Release` — zero warnings on net8.0/net9.0/net10.0
- [ ] `dotnet test -c Release` — all green on both test TFMs
- [ ] `dotnet format --verify-no-changes` clean
- [ ] No `TODO` / `NotImplementedException` in `src/` (`grep` the diff)

## Security

- [ ] `dotnet list package --vulnerable --include-transitive` clean
- [ ] CodeQL: no open high/critical alerts
- [ ] [Security checklist](../security/security-checklist.md) per-release section done

## Packaging

- [ ] `dotnet pack -c Release -o artifacts/packages` succeeds (package validation runs here)
- [ ] Each `.nupkg` contains: correct TFM libs + XML docs, README.md, icon.png, license expression; `.snupkg` present
- [ ] Local consumption smoke test passes ([how](../testing/compatibility-testing.md))

## Docs & metadata

- [ ] CHANGELOG `Unreleased` → version section with date
- [ ] Version bumped in `Directory.Build.props`
- [ ] README quick start compiles against this version
- [ ] Feature docs updated for anything that changed; ROADMAP refreshed

## Ship

- [ ] Release-prep PR merged, CI green
- [ ] Tag `v<version>` pushed; `nuget-release` environment approved
- [ ] NuGet.org pages verified; GitHub release notes sensible
- [ ] Post-release: milestone closed, next milestone opened
