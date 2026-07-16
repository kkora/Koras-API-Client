# Minimal API sample

An ASP.NET Core Minimal API proxying an upstream REST API through Koras.ApiClient, demonstrating: configuration-bound options (`appsettings.json` → `KorasApiClientOptions`), typed clients (`AddTypedClient<TodoGateway>`), per-request caching, and mapping `ApiError` categories to proper HTTP results.

## Run

```bash
dotnet run --project samples/MinimalApi.Sample
```

Then try:

- `GET http://localhost:5000/todos/1` → 200 with the upstream todo
- `GET http://localhost:5000/todos/999999` → 404 (upstream NotFound mapped cleanly)
- `GET http://localhost:5000/todos` → 200, second call within 30 s served from the response cache

## Expected output

Structured `Koras.ApiClient` log lines per upstream call (method, path, status, elapsed, correlation id) — with query strings and credentials never logged.

## Error scenarios

Change `PlaceholderApi:BaseAddress` to an unreachable host and restart: endpoints return RFC 9457 problem responses with 503 (transient upstream failure) after bounded retries.

## Configuration & secrets

Options bind from the `PlaceholderApi` section. For authenticated upstreams add `.WithApiKey(...)`/`.WithClientCredentials(...)` in `Program.cs` and store secrets with `dotnet user-secrets` or environment variables.
