# Dependency Injection — getting started

This page is the short version; the full guide is [features/dependency-injection.md](../features/dependency-injection.md).

```csharp
// one API → default client, inject IApiClient
services.AddKorasApiClient(o => o.BaseAddress = new Uri("https://api.example.com/"));

// several APIs → named clients via IApiClientFactory
services.AddKorasApiClient("github", o => o.BaseAddress = new Uri("https://api.github.com/"));

// domain-friendly → typed clients
services.AddKorasApiClient("orders", o => o.BaseAddress = ...)
    .AddTypedClient<OrderGateway>();
```

Everything composes on `HttpClientFactory`; the raw `IHttpClientBuilder` stays available at `builder.HttpClientBuilder` for anything the framework doesn't wrap.
