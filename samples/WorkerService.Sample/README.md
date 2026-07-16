# Worker Service sample

A hosted background service polling a page-number-paginated API (`_page`/`_limit` style) with `PaginateByPageNumberAsync`, demonstrating `IApiClientFactory` in long-lived services, streaming enumeration, bounded pages, graceful shutdown via the stopping token, and app-level handling of `ApiClientException` from failed pages.

## Run

```bash
dotnet run --project samples/WorkerService.Sample
```

## Expected output

```text
info: Koras.ApiClient.Samples.Worker.PostsPoller[0]
      Fetched 100 posts across all pages.
```

…repeated every 30 seconds. Press Ctrl+C for a clean cancellation-aware shutdown.

## Error scenarios

Disconnect the network: the cycle logs `Polling failed with Network/network_error; retrying next cycle.` and recovers automatically on the next tick — bounded retries happen inside each page request first.

## Configuration & secrets

None required. For authenticated APIs, add `.WithClientCredentials(...)` and store the client secret in environment variables or a secret store.
