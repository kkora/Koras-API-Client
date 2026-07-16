# ADR-0006: Ship a first-class Testing package

**Status:** Accepted · 2026-07-16

## Context

Consumers of HTTP client libraries habitually mock `HttpMessageHandler.SendAsync` (protected, awkward) or stand up wire servers (WireMock) for what should be unit tests. A framework that normalizes errors should also make *simulating* those errors trivial.

## Decision

Ship `Koras.ApiClient.Testing` with:

- `MockApiHandler` — fluent request matching (`When(method, path)`), JSON/problem/status/sequence/exception responses, full request recording (including bodies).
- `KorasApiClientTestFactory.Create(handler, configure)` — a real `KorasApiClient` over the mock transport, so tests exercise the true serialization/error pipeline, not a stub of `IApiClient`.

The package is test-framework-agnostic (no xUnit/NUnit dependency).

## Consequences

- The core's own unit tests consume the Testing package, keeping it honest ("dogfooding").
- `KorasApiClient`'s constructor stays public (documented for advanced/testing use) to avoid `InternalsVisibleTo` coupling.
