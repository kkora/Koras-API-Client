# Quick Start — five minutes to a resilient API call

## 1. Install

```bash
dotnet add package Koras.ApiClient
```

## 2. Register

```csharp
// Program.cs (any host: ASP.NET Core, worker, console)
builder.Services.AddKorasApiClient(options =>
{
    options.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
});
```

## 3. Call

```csharp
public sealed record Todo(int Id, int UserId, string Title, bool Completed);

public sealed class TodoService(IApiClient api)
{
    public async Task<Todo?> GetAsync(int id, CancellationToken ct)
    {
        var response = await api.Get($"todos/{id}").SendAsync<Todo>(ct);

        if (response.Error?.Category == ApiErrorCategory.NotFound)
            return null;                     // failures are data, not surprises

        return response.GetValueOrThrow();   // exceptions opt-in, single type: ApiClientException
    }
}
```

## What you already have (no extra config)

- ✅ Bounded retries with jitter, circuit breaker, timeouts (`Retry-After` honored)
- ✅ A correlation ID on every request, echoed on every response
- ✅ Structured logs with query strings and credentials redacted
- ✅ An OpenTelemetry-ready span and metrics per call
- ✅ Every failure normalized into a categorized `ApiError`

## Next steps

- Add auth: [authentication.md](../features/authentication.md) — `.WithApiKey(...)`, `.WithClientCredentials(...)`
- Bind options from `appsettings.json`: [configuration](configuration.md)
- Enumerate paginated APIs: [pagination.md](../features/pagination.md)
- Unit-test it: [testing-support.md](../features/testing-support.md)
- Run the [samples](../../samples/)
