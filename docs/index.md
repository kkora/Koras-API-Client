# Koras.ApiClient Documentation

A resilient, observable, secure-by-default HTTP API client framework for enterprise .NET.

## Start here

- [Installation](getting-started/installation.md) · [Quick start](getting-started/quick-start.md) · [Dependency injection](features/dependency-injection.md) · [Configuration](configuration/all-options.md)

## Features

| Guide | What it covers |
|---|---|
| [Request builder](features/request-builder.md) | Fluent requests, JSON/form/multipart bodies, headers, queries |
| [Error handling](features/error-handling.md) | `ApiResponse`/`ApiError`, categories, ProblemDetails, exceptions opt-in |
| [Authentication](features/authentication.md) | API key, bearer tokens, OAuth2 client credentials, managed identity seam |
| [Resilience](features/resilience.md) | Default-on retry, circuit breaker, timeouts, rate limits |
| [Pagination](features/pagination.md) | Cursor & page-number as `IAsyncEnumerable<T>` |
| [Response caching](features/response-caching.md) | Per-request & client-wide GET caching |
| [Observability](features/observability.md) | Logs, traces, metrics, correlation IDs, redaction |
| [Dependency injection](features/dependency-injection.md) | Default/named/typed clients, options, validation |
| [File transfer](features/file-transfer.md) | Streaming uploads & downloads with progress |
| [Testing support](features/testing-support.md) | `MockApiHandler`, request recording, DI transport swap |

## Deep dives

- [Architecture](architecture/overview.md) — layering, lifecycle, diagrams, [decision records](architecture/decision-records/)
- [Public API design](api/public-api-design.md) · [Backward compatibility](api/backward-compatibility.md)
- [Testing strategy](testing/test-strategy.md) · [Security & threat model](security/threat-model.md) · [Performance](performance/performance-guide.md)
- [Troubleshooting & FAQ](troubleshooting/common-errors.md) · [Release & versioning](release/versioning.md)

## Samples

[Console](../samples/Console.Sample/) · [Minimal API](../samples/MinimalApi.Sample/) · [Worker Service](../samples/WorkerService.Sample/)
