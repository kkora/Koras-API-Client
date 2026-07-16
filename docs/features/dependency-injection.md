# Dependency Injection & Configuration

## Overview

Registration mirrors `HttpClientFactory` idioms: a **default** client for single-API apps, **named** clients for many APIs, and **typed** clients for clean domain gateways. Options bind from `IConfiguration` and validate at startup.

## Single API (default client)

```csharp
services.AddKorasApiClient(options => options.BaseAddress = new Uri("https://api.example.com/"));
// inject IApiClient anywhere
```

## Multiple APIs (named clients)

```csharp
services.AddKorasApiClient("github", o => o.BaseAddress = new Uri("https://api.github.com/"));
services.AddKorasApiClient("stripe", o => o.BaseAddress = new Uri("https://api.stripe.com/"));

public sealed class BillingService(IApiClientFactory clients)
{
    private readonly IApiClient _stripe = clients.CreateClient("stripe");
}
```

## Typed clients (recommended for domain code)

```csharp
services.AddKorasApiClient("orders", o => o.BaseAddress = ...)
    .AddTypedClient<OrderGateway>();          // ctor receives this named IApiClient

public sealed class OrderGateway(IApiClient client)
{
    public Task<ApiResponse<Order>> GetAsync(string id, CancellationToken ct) =>
        client.Get($"orders/{id}").SendAsync<Order>(ct);
}
```

## Configuration binding

```json
{ "Clients": { "Vendor": { "BaseAddress": "https://api.vendor.com/", "Timeout": "00:00:20",
                            "CorrelationHeaderName": "X-Request-ID" } } }
```

```csharp
services.AddKorasApiClient("vendor", configuration.GetSection("Clients:Vendor"),
    options => options.DefaultHeaders["X-Client"] = "billing-service");   // code overrides config
```

## Startup validation

Invalid options (non-positive timeout, relative base address, missing API key, incomplete client-credentials) fail on host start via `ValidateOnStart` — not on the first request at 2 a.m.

## Lifetimes

| Service | Lifetime |
|---|---|
| `IApiClientFactory` | singleton |
| `IApiClient` (default registration) | singleton wrapper; safe everywhere |
| Clients from `CreateClient` | cheap transient wrappers over factory-managed handlers |
| Typed clients | transient |
| Token providers | singleton per named client |

`HttpClientFactory` owns handler lifetimes underneath — no socket exhaustion, DNS refresh respected.

## The escape hatch

`IKorasApiClientBuilder.HttpClientBuilder` is the raw `IHttpClientBuilder`: add custom `DelegatingHandler`s, configure the primary handler (this is how tests inject `MockApiHandler`), or apply any standard extension.

## Common mistakes

- Calling `AddKorasApiClient` twice with the same name (duplicate pipelines) — register once, configure via the builder.
- Resolving `IApiClient` directly when using only named registrations — the injectable `IApiClient` exists only via the default overload; otherwise use `IApiClientFactory` or a typed client.
