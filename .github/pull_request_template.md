# Pull Request

## What & why

<!-- What does this change, and what problem does it solve? Link the issue. -->

## Checklist

- [ ] Build green on net8.0/net9.0/net10.0 (`dotnet build -c Release`, zero warnings)
- [ ] Tests added/updated and executed (`dotnet test -c Release`)
- [ ] Public API change? → [API review checklist](../docs/api/public-api-review-checklist.md) applied
- [ ] Docs updated (`docs/features/*`, guides) + CHANGELOG `Unreleased` entry
- [ ] New dependency? → justification block below (why, alternatives, license, maintenance, security, size, API leakage)
- [ ] No secrets, credentials, or placeholder code (`TODO`, `NotImplementedException`) in the diff

## Breaking change?

- [ ] No
- [ ] Yes — follows [backward-compatibility process](../docs/api/backward-compatibility.md), migration notes included
