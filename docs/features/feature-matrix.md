# Feature Matrix

| Feature | Abstractions | Core (`Koras.ApiClient`) | Testing pkg | MVP | Tests | Docs | Example |
|---|---|---|---|---|---|---|---|
| F-001 Fluent request builder | `ApiRequest` | `ApiRequestBuilder`, verb extensions | — | ✅ | Unit + integration | features/request-builder.md | Console, MinimalApi |
| F-002 Error model | `ApiError`, `ApiErrorCategory`, `ProblemDocument`, `ApiClientException`, `ApiResponse` | error factory, problem parsing | `RespondWithProblem` | ✅ | Unit (mapping table) | features/error-handling.md | MinimalApi |
| F-003 API key auth | — | `ApiKeyAuthenticationHandler` + options | — | ✅ | Unit | features/authentication.md | Console |
| F-004 Bearer/token providers | `IAccessTokenProvider`, `AccessToken` | handler, static/delegate providers | — | ✅ | Unit | features/authentication.md | Worker |
| F-005 Client credentials | — | `ClientCredentialsTokenProvider` | token endpoint mocking | ✅ | Unit + integration | features/authentication.md | Worker |
| F-006 Resilience | — | standard resilience default-on, `ConfigureResilience` | — | ✅ | Integration (retry) | features/resilience.md | all |
| F-007 Pagination | `Page<T>` | `PaginateAsync`, `PaginateByPageNumberAsync` | — | ✅ | Unit + integration | features/pagination.md | Worker |
| F-008 Response caching | — | `ResponseCachingHandler`, `WithResponseCaching` | — | ✅ | Unit | features/response-caching.md | Console |
| F-009 Correlation IDs | `ApiResponse.CorrelationId` | generation + propagation | recorded headers | ✅ | Unit + integration | features/observability.md | MinimalApi |
| F-010 Observability | telemetry name constants | ActivitySource, Meter, logging | — | ✅ | Unit | features/observability.md | MinimalApi |
| F-011 DI & options | `IApiClient`, `IApiClientFactory` | registration, builder, validation | — | ✅ | Unit | features/dependency-injection.md | all |
| F-012 Upload/download | `DownloadAsync` on `IApiClient` | multipart builder, streaming download | — | ✅ | Unit + integration | features/file-transfer.md | Console |
| F-013 Testing support | — | — | `MockApiHandler`, `KorasApiClientTestFactory` | ✅ | Self-tested by suite | features/testing-support.md | — |
| F-014 Managed identity (Azure adapter) | seam ships in MVP | — | — | 0.5.0 | — | roadmap | — |
| F-015 Idempotency/signing | — | — | — | 1.x | — | roadmap | — |
| F-016 Distributed caching | — | — | — | 1.x | — | roadmap | — |
| F-017 Health checks pkg | — | — | — | 0.5.0 | — | roadmap | — |
| F-018 OpenAPI interop | — | — | — | 2.0 | — | roadmap | — |
| F-019 Offline queue | — | — | — | Experimental | — | roadmap | — |
