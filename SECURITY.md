# Security Policy

## Supported versions

| Version | Supported |
|---|---|
| 0.1.x (preview) | ✅ latest preview only |

Once 1.0 ships: latest major and the final minor of the previous major receive security fixes.

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Use GitHub's [private vulnerability reporting](https://github.com/KKORA/koras-api-client/security/advisories/new) for this repository. You will receive an acknowledgement within 72 hours and a remediation plan or resolution target within 14 days. Coordinated disclosure: we ask that you give us 90 days before public disclosure.

## Scope

In scope: credential handling (API keys, bearer tokens, client secrets), log/telemetry redaction, cache isolation, request forgery vectors through builder inputs, dependency vulnerabilities. Out of scope: vulnerabilities in the remote APIs you call, or in consumer misconfiguration explicitly warned against in the docs.

## Hardening posture

See [docs/security/threat-model.md](docs/security/threat-model.md) and [docs/security/security-checklist.md](docs/security/security-checklist.md). CI runs CodeQL, dependency review, and `dotnet list package --vulnerable --include-transitive` on every PR.
