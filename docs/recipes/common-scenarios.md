# Recipes — Common Scenarios

## Call a vendor API with an API key, map failures to domain results

```csharp
services.AddKorasApiClient("vendor", cfg.GetSection("Clients:Vendor"))
    .WithApiKey(k => k.ApiKey = cfg["Vendor:ApiKey"]!)
    .AddTypedClient<VendorGateway>();

public sealed class VendorGateway(IApiClient client)
{
    public async Task<Result<Quote>> GetQuoteAsync(string id, CancellationToken ct)
    {
        var response = await client.Get($"quotes/{id}").SendAsync<Quote>(ct);
        return response.Match(
            quote => Result<Quote>.Success(quote!),
            error => error.Category switch
            {
                ApiErrorCategory.NotFound => Result<Quote>.NotFound(),
                _ when error.IsTransient => Result<Quote>.Unavailable(error.RetryAfter),
                _ => Result<Quote>.Failure(error.Code),
            });
    }
}
```

## Fan out many calls with bounded concurrency

```csharp
var results = new ConcurrentBag<Order>();
await Parallel.ForEachAsync(orderIds, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct },
    async (id, token) =>
    {
        var response = await client.Get($"orders/{id}").SendAsync<Order>(token);
        if (response.IsSuccess) results.Add(response.Value!);
    });
```

The client is thread-safe; the resilience rate limiter and circuit breaker provide backpressure.

## Sync all pages into a database nightly

```csharp
await foreach (var customer in client.PaginateAsync<CustomerPage, Customer>(
    (c, cursor) => c.Get("customers").WithQuery("cursor", cursor).WithQuery("updated_since", since),
    page => new Page<Customer>(page.Items, page.NextCursor),
    maxPages: 5000, cancellationToken: stoppingToken))
{
    await repository.UpsertAsync(customer, stoppingToken);
}
```

## Respect Retry-After for app-level scheduling

```csharp
if (response.Error is { Category: ApiErrorCategory.RateLimited, RetryAfter: { } wait })
    await scheduler.RequeueAsync(job, delay: wait, ct);
```

## Two tenants, two credentials, one API

```csharp
foreach (var tenant in tenants)
    services.AddKorasApiClient($"crm-{tenant.Id}", o => o.BaseAddress = tenant.BaseAddress)
        .WithClientCredentials(o => { o.TokenEndpoint = tenant.Sts; o.ClientId = tenant.ClientId; o.ClientSecret = tenant.Secret; });
// resolve per request: factory.CreateClient($"crm-{tenantId}")
```
