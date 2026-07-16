# Naming Guidelines

- Root namespace `Koras.ApiClient`; sub-namespaces only when a cohesive area exceeds ~6 public types (`.Authentication`, `.Testing`). DI extensions live in `Microsoft.Extensions.DependencyInjection` (ecosystem convention for discoverability).
- Framework-defining types carry the `Koras` prefix (`KorasApiClient`, `KorasApiClientOptions`, `IKorasApiClientBuilder`) to avoid collisions in consumer code; payload/result types stay unprefixed (`ApiResponse`, `ApiError`, `Page<T>`) because they are typed constantly.
- Async methods end in `Async` and accept `CancellationToken` as the last parameter with a default.
- Fluent builder methods use `With*` (configuration) and imperative verbs for terminals (`SendAsync`, `DownloadAsync`, `Build`).
- DI registration: `Add*` on `IServiceCollection`, `With*`/`Configure*` on `IKorasApiClientBuilder`.
- Options classes end in `Options`; enums are singular (`ApiKeyLocation`, `ApiErrorCategory`).
- Boolean options read positively (`EnableCorrelationId`, not `DisableCorrelationId`).
- Metric names: `koras.apiclient.*` dotted lowercase per OpenTelemetry semantic-convention style; tags reuse OTel names (`http.request.method`, `http.response.status_code`) plus `koras.client.name`.
- Test naming: `Method_Scenario_Expectation` (e.g., `SendAsync_Returns404_MapsNotFoundCategory`).
