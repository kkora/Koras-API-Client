# Resilience (Retry, Circuit Breaker, Timeouts, Rate Limits)

## Overview

Every client registered with `AddKorasApiClient` gets the `Microsoft.Extensions.Http.Resilience` **standard pipeline** automatically: rate limiter → total-timeout → retry (exponential + jitter, honors `Retry-After`) → circuit breaker → per-attempt timeout. The easy path is the safe path ([ADR-0004](../architecture/decision-records/adr-0004-resilience-composition.md)).

## Defaults you should know

| Layer | Default |
|---|---|
| Total request timeout | 30 s |
| Retries | 3 attempts, exponential backoff + jitter, `Retry-After` respected |
| Circuit breaker | opens on high failure ratio (10 % over 30 s, min 100 ops) |
| Attempt timeout | 10 s per try |
| `KorasApiClientOptions.Timeout` | 30 s cap on the underlying `HttpClient` |

Retries apply to idempotent scenarios and transient results (5xx, 408, 429, network failures) per the standard handler's rules.

## Tuning

```csharp
services.AddKorasApiClient("partner", o => { o.BaseAddress = ...; o.Timeout = TimeSpan.FromMinutes(3); })
    .ConfigureResilience(resilience =>
    {
        resilience.Retry.MaxRetryAttempts = 5;
        resilience.Retry.Delay = TimeSpan.FromMilliseconds(200);
        resilience.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);   // keep aligned with options.Timeout
        resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
        resilience.CircuitBreaker.FailureRatio = 0.2;
    });
```

For long-running operations raise **both** `KorasApiClientOptions.Timeout` and the resilience timeouts — the smallest budget wins.

## Per-request timeout

`WithTimeout(TimeSpan)` bounds one logical operation (all retries included) and yields a `Timeout`-category `ApiError` when exceeded.

## Rate-limit handling

429 responses: the retry strategy waits for `Retry-After` automatically; if retries are exhausted you receive `ApiErrorCategory.RateLimited` with `ApiError.RetryAfter` populated for app-level scheduling.

## Opting out / advanced pipelines

The standard handler is attached at registration. For fully custom Polly pipelines, add them on the escape hatch — `builder.HttpClientBuilder` — with the standard `AddResilienceHandler(...)` APIs.

## Telemetry

Each retry attempt is visible in logs (Polly event source) and as per-attempt `System.Net.Http` spans nested inside the single Koras logical span.

## Testing

The integration suite proves 503→503→200 succeeds through retries (`EndToEndTests.FlakyEndpoint_SucceedsThroughDefaultRetries`). In tests, shrink `Retry.Delay` via `ConfigureResilience` to keep runs fast.

## Common mistakes

- Wrapping `SendAsync` in your own retry loop — you'll multiply retries (3 × 3 = 9 attempts). Configure the pipeline instead.
- Raising `options.Timeout` without raising `TotalRequestTimeout` (the 30 s default still cancels first).
- Retrying non-transient categories app-side; branch on `error.IsTransient`.
