# Response Caching

## Overview

Opt-in, memory-backed caching of successful GET responses, keyed by client name + full request URI. Two activation modes: per request (`WithCache`) or client-wide (`WithResponseCaching`). The caching handler sits outermost in the pipeline, so cache hits skip auth, resilience, and the network entirely.

## Per-request (works with zero extra registration)

```csharp
var countries = await client.Get("reference/countries")
    .WithCache(TimeSpan.FromHours(1))
    .SendAsync<List<Country>>(ct);
```

## Client-wide

```csharp
services.AddKorasApiClient("reference", o => o.BaseAddress = ...)
    .WithResponseCaching(cache =>
    {
        cache.DefaultDuration = TimeSpan.FromMinutes(5);
        cache.MaxEntrySizeBytes = 512 * 1024;
    });
```

## Rules

- **GET only**; only **HTTP 200** responses are stored.
- Bodies larger than `MaxEntrySizeBytes` (default 1 MiB) pass through uncached.
- Entries expire after the TTL (absolute).
- The store is the app's `IMemoryCache` (registered automatically).

## When not to use it

- Endpoints whose response varies by **user or tenant** while the URI stays identical — the key is the URI, so responses would leak across principals. Include the variance in the URI, or don't cache.
- Rapidly changing data where staleness beats latency savings.
- Multi-instance freshness requirements (distributed `IDistributedCache` backend is on the roadmap).

## Security considerations

Treat cached bodies as plaintext in process memory. Never cache responses containing per-user secrets. The multi-tenant caution above is the primary risk — reviewed in the [threat model](../security/threat-model.md).

## Performance

A cache hit costs a dictionary lookup plus response rehydration from `byte[]` — typically microseconds versus a network round trip.

## Testing

`CachingTests` show the pattern: register the client in DI, swap the primary handler for `MockApiHandler`, and assert on `mock.Requests.Count`.

## Common mistakes

- Expecting `WithCache` on POST to work — non-GETs always pass through.
- Assuming HTTP `Cache-Control` headers drive behavior — durations are explicit and app-controlled in this release.
