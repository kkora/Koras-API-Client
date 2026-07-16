# Backlog

Epics → stories → tasks. IDs are stable; completion criteria are binary.

## Epic E0 — Repository foundation

- **T-000** Solution, `Directory.Build.props` (TFMs, warnings-as-errors, nullable, deterministic, SourceLink), `Directory.Packages.props` (CPM), `global.json`, `.editorconfig`, `.gitignore`, `.gitattributes`, `NuGet.Config`. *Files:* root. *Done:* clean `dotnet build`.
- **T-001** Community/docs files: README, LICENSE, CONTRIBUTING, CODE_OF_CONDUCT, SECURITY, SUPPORT, CHANGELOG, ROADMAP, CLAUDE.md. *Done:* files exist with real content.
- **T-002** GitHub: workflows (build/test/package/release/codeql/dependency-review), issue templates, PR template, dependabot. *Done:* YAML lints.

## Epic E1 — Abstractions (Milestone 1)

- **T-101** `ApiError`, `ApiErrorCategory`, `ProblemDocument`, `ApiClientException`, `ApiResponse`, `ApiResponse<T>`. *Tests:* response semantics, EnsureSuccess, Match. *Risk:* nullability ergonomics of `Value`.
- **T-102** `IApiClient`, `IApiClientFactory`, `ApiRequest`, `IAccessTokenProvider`, `AccessToken`, `Page<T>`, `KorasApiClientTelemetry` constants. *Tests:* via core suite.

## Epic E2 — Core client (Milestone 2)

- **T-201** `KorasApiClient`: URI composition (query escaping, path+query merge), header application, correlation, per-request timeout linking, Activity/metrics, exception→error mapping, ProblemDetails parse, bounded body capture, `SendAsync<T>` deserialization incl. string/204 handling. *Tests:* mapping table, timeout vs cancellation, serialization.
- **T-202** `ApiRequestBuilder`, verb extensions, `MultipartBuilder`, form/json content, `DownloadAsync` streaming with progress. *Tests:* builder branches, multipart round-trip, download.

## Epic E3 — DI & configuration (Milestone 3)

- **T-301** `AddKorasApiClient` overloads, `IKorasApiClientBuilder`, `KorasApiClientFactory`, options validator + `ValidateOnStart`, config binding, `AddTypedClient<T>`. *Tests:* resolution, named isolation, validation failure, binding.

## Epic E4 — Auth & pipeline (Milestone 4)

- **T-401** `ApiKeyAuthenticationHandler` + options (header/query). *Tests:* placement, validation.
- **T-402** `BearerTokenHandler`, `StaticTokenProvider`, `DelegateTokenProvider`; 401 single-retry semantics. *Tests:* attach, refresh-on-401, no-retry-with-content.
- **T-403** `ClientCredentialsTokenProvider`: cache, skew, single-flight, error surface. *Tests:* caching, concurrency, failure mapping.
- **T-404** `ResponseCachingHandler` + `WithResponseCaching`. *Tests:* hit/miss/TTL/size/non-GET.

## Epic E5 — Pagination & resilience (Milestone 5)

- **T-501** `PaginateAsync`, `PaginateByPageNumberAsync` with maxPages + cancellation. *Tests:* unit + integration.
- **T-502** Standard resilience default wiring + `ConfigureResilience`. *Tests:* integration retry proof.

## Epic E6 — Observability (Milestone 6)

- **T-601** LoggerMessage logging with redaction; Activity tags; Meter instruments. *Tests:* ActivityListener/MeterListener assertions, no-secret logs.

## Epic E7 — Testing package & samples (Milestone 7)

- **T-701** `MockApiHandler`, `MockRequestSetup`, `RecordedRequest`, `KorasApiClientTestFactory`.
- **T-702** Samples: Console, MinimalApi, WorkerService with READMEs.

## Epic E8 — Hardening (Milestone 8)

- **T-801** Security docs + checklist pass; vulnerability audit.
- **T-802** Benchmarks project (URI composition, send-over-mock, serialization).
- **T-803** Architecture tests (dependency direction, namespaces, async hygiene).

## Epic E9 — Packaging (Milestone 9)

- **T-901** Package metadata, per-package READMEs, icon, pack validation, symbols; release docs.
