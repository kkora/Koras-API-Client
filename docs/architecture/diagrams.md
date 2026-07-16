# Architecture Diagrams

## Package dependency diagram

```mermaid
flowchart BT
    ABS["Koras.ApiClient.Abstractions\n(BCL only)"]
    CORE["Koras.ApiClient\n(Microsoft.Extensions.*)"]
    TEST["Koras.ApiClient.Testing"]
    AZ["Koras.ApiClient.Azure (0.5.0)"]
    CORE --> ABS
    TEST --> CORE
    AZ -.-> ABS
```

## Component architecture

```mermaid
flowchart TD
    subgraph DI["Dependency Injection"]
        REG[AddKorasApiClient] --> B[IKorasApiClientBuilder]
        B --> HCF[IHttpClientBuilder / HttpClientFactory]
        FAC[IApiClientFactory]
    end
    subgraph Fluent["Fluent layer"]
        VERBS[Get/Post/Put/Patch/Delete/Head] --> RB[ApiRequestBuilder] --> REQ[ApiRequest]
        PAG[PaginateAsync]
    end
    FAC --> CLIENT[KorasApiClient : IApiClient]
    REQ --> CLIENT
    CLIENT --> PIPE[Handler pipeline]
    subgraph PIPE["Handler pipeline"]
        AUTH[Auth handlers] --> CACHE[ResponseCachingHandler] --> RES[Standard resilience] --> HTTP[SocketsHttpHandler]
    end
    CLIENT --> ERR[ApiErrorFactory] --> RESP[ApiResponse / ApiError]
    CLIENT --> TEL[ActivitySource + Meter + ILogger]
```

## Request lifecycle (sequence)

```mermaid
sequenceDiagram
    participant App
    participant Builder as ApiRequestBuilder
    participant Client as KorasApiClient
    participant Handlers as Auth→Cache→Resilience
    participant API as Remote API
    App->>Builder: Get("orders").WithQuery(...)
    App->>Builder: SendAsync<OrderList>(ct)
    Builder->>Client: SendAsync<T>(ApiRequest, ct)
    Client->>Client: compose URI, correlation id, start Activity
    Client->>Handlers: HttpRequestMessage
    Handlers->>API: HTTPS (retried/circuit-broken as needed)
    API-->>Handlers: 200 / 4xx / 5xx
    Handlers-->>Client: HttpResponseMessage
    alt success
        Client-->>App: ApiResponse<T> (Value)
    else failure
        Client->>Client: parse ProblemDetails, map category
        Client-->>App: ApiResponse<T> (Error)
    end
```

## Provider (token) lifecycle

```mermaid
sequenceDiagram
    participant H as BearerTokenHandler
    participant P as ClientCredentialsTokenProvider
    participant STS as Token endpoint
    H->>P: GetTokenAsync(forceRefresh=false)
    alt cached & fresh
        P-->>H: AccessToken
    else expired/missing
        P->>P: semaphore (single flight)
        P->>STS: POST grant_type=client_credentials
        STS-->>P: access_token, expires_in
        P-->>H: AccessToken (cached, skew-adjusted)
    end
    H->>H: attach Authorization: Bearer
    Note over H: on 401 (bodiless request): force refresh, retry once
```

## Dependency-injection flow

```mermaid
flowchart LR
    A[appsettings.json] -->|Bind| O["IOptionsMonitor&lt;KorasApiClientOptions&gt; (named)"]
    C[AddKorasApiClient] --> O
    C --> H["AddHttpClient('Koras.ApiClient.name')"]
    H --> P[handler pipeline registrations]
    O --> V[IValidateOptions → startup validation]
    F[IApiClientFactory.CreateClient] --> H
    F --> O
    F --> K[KorasApiClient]
    T["AddTypedClient&lt;TClient&gt;"] --> F
```

Error lifecycle diagram: see [error-model.md](error-model.md). Telemetry flow: see [observability.md](observability.md).
