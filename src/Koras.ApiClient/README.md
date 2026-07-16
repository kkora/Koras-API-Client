# Koras.ApiClient

A resilient, observable, secure-by-default HTTP API client framework for enterprise .NET applications — authentication, retries, circuit breaking, pagination, caching, correlation IDs, telemetry, and normalized errors, composed on `HttpClientFactory`.

## Quick start

```csharp
builder.Services
    .AddKorasApiClient(options => options.BaseAddress = new Uri("https://api.example.com/"))
    .WithApiKey(key => key.ApiKey = builder.Configuration["Example:ApiKey"]!);
```

```csharp
public sealed class TodoService(IApiClient api)
{
    public async Task<Todo?> GetAsync(int id, CancellationToken ct)
    {
        ApiResponse<Todo> response = await api.Get($"todos/{id}").SendAsync<Todo>(ct);

        if (response.Error?.Category == ApiErrorCategory.NotFound)
            return null;

        return response.GetValueOrThrow();
    }
}
```

Every request gets bounded retries with jitter, a circuit breaker, timeouts, a correlation ID, structured logs, and an OpenTelemetry-ready span and metrics — with zero extra configuration. Every failure (network, timeout, 4xx/5xx, deserialization) is a categorized `ApiError`, never an unhandled exception.

## Highlights

- **Fluent requests** — `client.Post("orders").WithJsonBody(order).SendAsync<OrderCreated>(ct)`
- **Auth included** — API key, bearer tokens (`IAccessTokenProvider`), OAuth 2.0 client credentials with cached single-flight refresh
- **Pagination** — `client.PaginateAsync(...)` as `IAsyncEnumerable<T>` with bounded pages
- **Testable** — pair with [`Koras.ApiClient.Testing`](https://www.nuget.org/packages/Koras.ApiClient.Testing) to unit-test without a network

Documentation: https://github.com/KKORA/koras-api-client/tree/main/docs
