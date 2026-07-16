# Roadmap

High-level plan; details in [docs/product/product-roadmap.md](docs/product/product-roadmap.md) and [docs/features/future-roadmap.md](docs/features/future-roadmap.md).

- **0.1.0-preview.1** — MVP: fluent builder, auth (API key / bearer / client credentials), default-on resilience, pagination, caching, correlation, telemetry, normalized errors, DI, testing package.
- **0.1.0** — API stabilization from preview feedback.
- **0.5.0** — `Koras.ApiClient.Azure` (managed identity), health-check integration package, distributed caching groundwork.
- **1.0.0** — frozen public API with baseline enforcement, performance-tuned hot paths, complete docs site.
- **1.x** — idempotency keys, request signing, bulkhead presets, `IDistributedCache` caching.
- **2.0 / experimental** — OpenAPI interop, offline queue, contract-testing helpers.
