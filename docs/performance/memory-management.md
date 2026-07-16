# Memory Management

## Buffering rules (what gets held in memory, ever)

| Operation | Memory behavior |
|---|---|
| `SendAsync` / `SendAsync<T>` | response buffered by `HttpClient` (`ResponseContentRead`) — intended for API-sized JSON payloads |
| `DownloadAsync` | **never buffers** — headers-read + pooled 80 KB copy buffer, O(1) memory for any body size |
| Multipart upload | file parts stream from the source `Stream` |
| Error bodies | captured up to `MaxErrorResponseBodyLength` (4 KB default), hard 1 MB read cap, skippable |
| Response cache | `byte[]` per entry, capped at `MaxEntrySizeBytes` (1 MiB default), TTL-evicted by `IMemoryCache` |
| Pagination | one page materialized at a time |

**Rule of thumb:** anything that might exceed a few MB goes through `DownloadAsync`/`WithContent(StreamContent)`, not `SendAsync<T>`.

## Pooling & allocation

Copy buffers rent from `ArrayPool<byte>.Shared` (returned in `finally`); metric tags use `TagList` (stack-allocated up to 8 tags); log messages are source-generated (zero cost below the active level); the builder allocates one `ApiRequest` + small collections per request — fine for request-scoped use, don't cache builders.

## Lifetime & disposal

`HttpResponseMessage`s are disposed by the client after normalization; upload streams are disposed with the request content (pass a fresh stream per attempt); `ClientCredentialsTokenProvider` implements `IDisposable` (semaphore) and is disposed by the container.

## Diagnosing leaks in consuming apps

The usual suspect is captured `ApiResponse.ResponseBody`/`Headers` kept alive in app-level caches — copy what you need, drop the response. `dotnet-counters monitor System.Runtime Microsoft.Extensions.Caching.Memory` shows cache entry counts if response caching is suspected.
