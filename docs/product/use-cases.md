# Primary Use Cases

## UC-1: REST API integration with an external vendor

An ASP.NET Core service calls a vendor REST API using an API key. The team needs retries with jitter, a timeout budget, structured logs, and typed errors — without writing handlers.

```csharp
services.AddKorasApiClient("vendor", o => o.BaseAddress = new Uri("https://api.vendor.com/"))
        .WithApiKey(k => k.ApiKey = builder.Configuration["Vendor:ApiKey"]!);
```

## UC-2: OAuth 2.0 client-credentials API

A backend service calls a partner API secured with OAuth client credentials. Tokens must be cached, refreshed before expiry, and retried once on 401.

```csharp
services.AddKorasApiClient("partner", o => o.BaseAddress = new Uri("https://api.partner.com/"))
        .WithClientCredentials(c =>
        {
            c.TokenEndpoint = new Uri("https://login.partner.com/oauth/token");
            c.ClientId = cfg["Partner:ClientId"]!;
            c.ClientSecret = cfg["Partner:ClientSecret"]!;
            c.Scopes = ["read:orders"];
        });
```

## UC-3: Paginated API consumption

A worker service enumerates all records from a cursor-paginated API as `IAsyncEnumerable<T>`, with cancellation and a bounded page limit.

```csharp
await foreach (var order in client.PaginateAsync<OrderPage, Order>(
    (c, cursor) => c.Get("orders").WithQuery("cursor", cursor),
    page => new Page<Order>(page.Items, page.NextCursor),
    cancellationToken: stoppingToken))
{ ... }
```

## UC-4: Resilient microservice-to-microservice communication

Internal services call each other with standard resilience (retry + circuit breaker + timeout), correlation-ID propagation, and OpenTelemetry traces so a request can be followed across the fleet.

## UC-5: Foundation for a vendor SDK

An SDK author builds `Contoso.Crm.Sdk` on Koras.ApiClient: they get auth, resilience, errors, and telemetry for free and expose only domain methods. `Koras.ApiClient.Testing`'s `MockApiHandler` lets them unit-test the SDK without a network.

## UC-6: File upload/download

Uploading multipart documents to a storage API and streaming large downloads to disk with progress reporting and cancellation, without buffering whole files in memory.
