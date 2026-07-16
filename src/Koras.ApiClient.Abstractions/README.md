# Koras.ApiClient.Abstractions

Contracts and models for [Koras.ApiClient](https://github.com/KKORA/koras-api-client) — the resilient, observable HTTP API client framework for .NET.

This package contains only interfaces and data types (`IApiClient`, `ApiResponse<T>`, `ApiError`, `ProblemDocument`, `IAccessTokenProvider`, `Page<T>`) with **zero third-party dependencies**. Reference it from SDKs and domain assemblies that consume an API client; applications install the full [`Koras.ApiClient`](https://www.nuget.org/packages/Koras.ApiClient) package.

```csharp
public sealed class OrderGateway(IApiClient client)
{
    public async Task<Order?> FindAsync(string id, CancellationToken ct)
    {
        ApiResponse<Order> response = await client.SendAsync<Order>(
            new ApiRequest(HttpMethod.Get, $"orders/{id}"), ct);

        return response.Error?.Category == ApiErrorCategory.NotFound
            ? null
            : response.GetValueOrThrow();
    }
}
```

Documentation: https://github.com/KKORA/koras-api-client/tree/main/docs
