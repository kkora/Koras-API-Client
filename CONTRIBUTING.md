# Contributing to Koras.ApiClient

Thank you for helping build a better HTTP client framework for .NET!

## Getting started

1. Install the .NET 10 SDK (`global.json` pins the feature band; .NET 8 & 9 runtimes are needed to run all test targets).
2. `git clone https://github.com/KKORA/koras-api-client && cd koras-api-client`
3. `dotnet build` then `dotnet test` — everything should be green before you start.

## Development workflow

- Branch from `main`: `feature/<topic>` or `fix/<topic>`.
- Keep PRs focused; one logical change per PR.
- Follow the coding rules in [CLAUDE.md](CLAUDE.md) and [docs/api/naming-guidelines.md](docs/api/naming-guidelines.md).
- Every public-surface change must pass [docs/api/public-api-review-checklist.md](docs/api/public-api-review-checklist.md).
- Add tests (happy, invalid-input, failure, cancellation) — see [docs/testing/test-strategy.md](docs/testing/test-strategy.md).
- Update the relevant `docs/features/*.md` guide and the `Unreleased` section of [CHANGELOG.md](CHANGELOG.md).

## Before pushing

```bash
dotnet build --configuration Release   # zero warnings — warnings are errors
dotnet test  --configuration Release
dotnet format --verify-no-changes
```

## Adding a dependency

Include a justification block in the PR (why, alternatives, license, maintenance, security, size, API leakage). See [docs/architecture/dependency-rules.md](docs/architecture/dependency-rules.md). Abstractions must remain BCL-only.

## Reporting bugs / requesting features

Use the issue templates. For security vulnerabilities, **do not open an issue** — see [SECURITY.md](SECURITY.md).

## Code of conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). Be excellent to each other.
