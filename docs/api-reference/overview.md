# API Reference Overview

Full IntelliSense documentation ships in the packages (XML docs on every public member). This page maps the surface:

## Koras.ApiClient.Abstractions — namespace `Koras.ApiClient`

`IApiClient` · `IApiClientFactory` · `ApiRequest` · `ApiResponse` / `ApiResponse<T>` · `ApiError` · `ApiErrorCategory` · `ProblemDocument` · `ApiClientException` · `IAccessTokenProvider` · `AccessToken` · `Page<T>` · `KorasApiClientTelemetry`

## Koras.ApiClient — namespaces `Koras.ApiClient`, `Koras.ApiClient.Authentication`, `Microsoft.Extensions.DependencyInjection`

- Fluent: `ApiClientExtensions` (verbs + `GetFromJsonAsync`/`PostJsonAsync`) · `ApiRequestBuilder` · `MultipartBuilder` · `ApiClientPaginationExtensions`
- Client & options: `KorasApiClient` · `KorasApiClientOptions` · `ResponseCachingOptions` · `KorasHttpRequestOptions`
- Authentication: `ApiKeyAuthenticationHandler`/`Options`/`ApiKeyLocation` · `BearerTokenHandler` · `StaticTokenProvider` · `DelegateTokenProvider` · `ClientCredentialsTokenProvider`/`Options`
- DI: `KorasApiClientServiceCollectionExtensions.AddKorasApiClient(…)` · `IKorasApiClientBuilder` · `KorasApiClientBuilderExtensions` (`WithApiKey`, `WithBearerToken`, `WithStaticBearerToken`, `WithClientCredentials`, `WithResponseCaching`, `ConfigureResilience`, `AddTypedClient`)

## Koras.ApiClient.Testing — namespace `Koras.ApiClient.Testing`

`MockApiHandler` · `MockRequestSetup` · `RecordedRequest` · `KorasApiClientTestFactory`

Design rationale and per-type contracts: [docs/api/public-api-design.md](../api/public-api-design.md). A generated reference site (DocFX) is planned for 1.0.
