# Feature Catalog — Koras.ApiClient

Each feature below follows the standard schema: problem, story, requirements, API, errors, security, performance, observability, tests, docs, acceptance criteria, and release classification. Detailed per-feature guides live in `docs/features/<feature>.md`.

---

## F-001 Fluent request builder

- **User problem:** Composing `HttpRequestMessage` (URI escaping, query strings, headers, content) is verbose and error-prone.
- **User story:** As an API developer, I build a request fluently (`client.Get("orders").WithQuery("status", "open").SendAsync<OrderList>()`) so integration code reads like the API call it makes.
- **Business value:** Core DX; every other feature hangs off it.
- **Functional requirements:** verbs GET/POST/PUT/PATCH/DELETE/HEAD; query params (escaped, multi-value); headers; JSON body; form body; multipart; raw `HttpContent`; per-request timeout, correlation ID, cache duration; terminal `SendAsync`, `SendAsync<T>`, `DownloadAsync`.
- **Nonfunctional:** zero hidden network calls before terminal method; allocation-light; thread-safe client, non-thread-safe builder (documented).
- **Public API:** `ApiRequestBuilder`, `ApiClientExtensions` verb methods, `ApiRequest`, `MultipartBuilder`.
- **Errors:** invalid path → `ArgumentException` at build; everything network-related normalized into `ApiError`.
- **Security:** query/header values escaped; no secrets required in URLs.
- **Observability:** logical operation span + metrics recorded per send.
- **Tests:** unit (query escaping, multi-value, header merge, body serialization, per-request timeout), integration (echo server round-trip).
- **Acceptance:** builder covers all verbs and content types with 100% branch coverage on URI composition. **MVP.**

## F-002 Normalized error model

- **User problem:** HTTP failures surface inconsistently (exceptions, nulls, status codes), forcing defensive code.
- **User story:** As a developer, every failure becomes an `ApiError` with a `Category` I can switch on, and `EnsureSuccess()` throws a single exception type when I prefer exceptions.
- **Functional:** categories (Network, Timeout, Serialization, Validation, Authentication, Authorization, NotFound, Conflict, RateLimited, Client, Server, Unknown); RFC 9457 `ProblemDocument` parsing; `Retry-After` extraction; bounded raw-body capture; `IsTransient` hint; `ApiClientException` for throw-style consumption.
- **Security:** response body capture bounded (default 4 KB) to avoid logging/memory blowups; no request secrets included in errors.
- **Tests:** mapping table per status; problem+json parsing; malformed problem JSON; network/timeout/cancellation distinctions. **MVP.**

## F-003 Authentication — API key

- **Story:** As a developer, I attach an API key as a header (default) or query parameter with one builder call.
- **API:** `WithApiKey(Action<ApiKeyAuthenticationOptions>)`; options: `ApiKey`, `HeaderName` (default `X-Api-Key`), `Location` (Header|QueryString), `QueryParameterName`.
- **Security:** header placement is default; query placement requires explicit opt-in (keys in URLs leak into logs); key never logged.
- **Tests:** header set; query set; validation failure when key empty. **MVP.**

## F-004 Authentication — bearer tokens & token providers

- **Story:** As a developer, I plug any token source via `IAccessTokenProvider` and the client attaches `Authorization: Bearer` and retries once on 401 with a force-refreshed token.
- **API:** `IAccessTokenProvider`, `AccessToken`, `WithBearerToken<TProvider>()`, `WithBearerToken(Func<IServiceProvider, IAccessTokenProvider>)`, `WithStaticBearerToken(string)` (dev/test).
- **Behavior:** 401 retry only when the request has no content (cannot safely replay consumed streams) — documented.
- **Security:** tokens never logged; managed-identity integration via the provider seam (Azure adapter deferred to `Koras.ApiClient.Azure`). **MVP.**

## F-005 Authentication — OAuth 2.0 client credentials

- **Story:** As a developer, I configure token endpoint + client id/secret/scopes and get cached tokens with single-flight refresh and expiry skew.
- **API:** `WithClientCredentials(Action<ClientCredentialsOptions>)`; `ClientCredentialsTokenProvider` public for advanced use.
- **Behavior:** token cached until `expires_in − RefreshSkew` (default 60 s); concurrent callers share one refresh (semaphore); refresh uses its own named `HttpClient`.
- **Errors:** token endpoint failure → `ApiError` Category `Authentication` on the outbound call.
- **Security:** secret held in options (sourced from configuration/secret store by the consumer); never logged; form-encoded POST only over the configured endpoint. **MVP.**

## F-006 Resilience (retry, timeout, circuit breaker, rate limits)

- **Story:** As a platform engineer, every client gets bounded retry with jitter, a circuit breaker, and total/attempt timeouts by default, honoring `Retry-After` on 429/503.
- **Implementation:** `Microsoft.Extensions.Http.Resilience` standard handler, enabled by `AddKorasApiClient`; tuned via `ConfigureResilience(Action<HttpStandardResilienceOptions>)`.
- **Nonfunctional:** no unbounded retries; non-idempotent requests follow the standard handler's safe defaults.
- **Tests:** integration test proving 503→503→200 succeeds through retries; timeout produces `Timeout` category. **MVP.**

## F-007 Pagination

- **Story:** As a developer, I consume any paginated API as `IAsyncEnumerable<T>` with cursor or page-number strategies, bounded pages, and cancellation.
- **API:** `PaginateAsync<TPage,TItem>(requestFactory, pageSelector, maxPages, ct)`, `PaginateByPageNumberAsync<TPage,TItem>(...)`, `Page<T>` record.
- **Errors:** any page failure → `ApiClientException` (enumeration cannot silently truncate); `maxPages` guard (default 1000) prevents unbounded loops.
- **Tests:** 3-page cursor walk, empty first page, mid-stream error, cancellation mid-enumeration, maxPages guard. **MVP.**

## F-008 Response caching

- **Story:** As a developer, I opt in to memory caching of GET responses per request (`WithCache(TimeSpan)`) or per client (options), keyed by full request URI.
- **Behavior:** GET + 200 only; bounded entry size (default 1 MB); reconstructs status/headers/body on hit.
- **Security:** cache key includes client name; never caches responses to authenticated-user-variant endpoints unless the caller opts in (documented multi-tenant caution).
- **Tests:** hit/miss, TTL expiry, non-GET bypass, size-limit bypass. **MVP.**

## F-009 Correlation IDs

- **Story:** As an operator, every outbound request carries `X-Correlation-ID` (configurable name), generated when absent, echoed on `ApiResponse.CorrelationId`, and stamped on logs/traces.
- **Tests:** auto-generation, explicit `WithCorrelationId`, disabled mode. **MVP.**

## F-010 Observability (logging, tracing, metrics)

- **Story:** As an operator, I get structured logs (`Koras.ApiClient` category), an `ActivitySource` span per logical operation (including retries beneath it), and `Meter` instruments (`koras.apiclient.requests`, `koras.apiclient.request.duration`) with client/method/status/error tags.
- **Security:** URLs logged without query strings by default; `Authorization`/API-key headers never logged.
- **OpenTelemetry:** consumers call `AddSource(KorasApiClientTelemetry.ActivitySourceName)` / `AddMeter(...MeterName)`.
- **Tests:** activity created with tags; metrics recorded; log redaction. **MVP.**

## F-011 Dependency injection & options

- **Story:** As a developer, `AddKorasApiClient(name, configure)` registers a named client on `HttpClientFactory`; typed clients via `AddTypedClient<TClient>`; options bindable from `IConfiguration` and validated at startup.
- **API:** `KorasApiClientServiceCollectionExtensions`, `IKorasApiClientBuilder`, `IApiClientFactory`, `KorasApiClientOptions`.
- **Tests:** resolve named/typed clients, options validation failures, configuration binding. **MVP.**

## F-012 File upload & download

- **Story:** As a developer, I upload multipart files from streams and download large responses directly to a `Stream` with `IProgress<long>` and cancellation, never buffering entire payloads.
- **Tests:** multipart round-trip, download-to-stream with progress, cancellation mid-download. **MVP.**

## F-013 Testing support (`Koras.ApiClient.Testing`)

- **Story:** As a consumer, I unit-test code that uses `IApiClient` without a network: `MockApiHandler` scripts responses/sequences/exceptions and records requests; `KorasApiClientTestFactory.Create(handler)` builds a real client over it.
- **Tests:** the package tests itself via the core test suite. **MVP.**

## F-014 Managed identity integration (Azure)

- Adapter package `Koras.ApiClient.Azure` bridging `Azure.Core.TokenCredential` → `IAccessTokenProvider`. Core stays Azure-free; the seam ships in MVP, the adapter in **0.5.0**.

## F-015 Idempotency keys / request signing — **1.x**
## F-016 Distributed response caching (`IDistributedCache`) — **1.x**
## F-017 Health-check integration package — **0.5.0**
## F-018 OpenAPI client generation interop — **2.0 / experimental**
## F-019 Offline queue — **experimental**
## F-020 Bulkhead isolation — **1.x (exposed via ConfigureResilience today)**

## Out of scope

GraphQL/gRPC/SOAP transports, XML deserialization in core, UI frameworks, API gateway features.
