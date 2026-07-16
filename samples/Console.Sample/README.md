# Console sample

Demonstrates the Koras.ApiClient fundamentals in a plain console app: DI registration, the fluent builder, errors-as-data, JSON bodies, response caching, and correlation IDs.

## Run

```bash
dotnet run --project samples/Console.Sample            # online, against jsonplaceholder.typicode.com
dotnet run --project samples/Console.Sample -- --offline   # no network: transport swapped for MockApiHandler
```

## Expected output (online)

```text
1) GET todos/1        -> #1 "delectus aut autem"
2) GET todos/999999   -> NotFound (http_404) as expected
3) POST todos         -> created #201
4) Cached GET         -> served again: "delectus aut autem" (no second HTTP request)
5) Correlation ID     -> 3f2a…
```

## Error scenarios

Disconnect your network and run without `--offline`: every call completes with `ApiErrorCategory.Network` errors after bounded retries — no unhandled exceptions.

## Configuration & secrets

This sample needs none. For APIs requiring credentials, see [docs/features/authentication.md](../../docs/features/authentication.md) and use user secrets or environment variables — never commit keys.

The project references the local sources; to consume the released package instead, replace the `ProjectReference` items with `<PackageReference Include="Koras.ApiClient" Version="…" />`.
