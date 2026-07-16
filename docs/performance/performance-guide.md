# Performance Guide

## Where time actually goes

A logical request = framework overhead (µs) + handler pipeline (µs) + **network (ms–s)**. The framework's job is to stay invisible next to the network and to prevent the pathological cases (buffering gigabytes, unbounded retries, socket exhaustion).

## Built-in performance properties

- **Socket hygiene** — `HttpClientFactory` handler pooling/rotation; clients are cheap wrappers, safe to create per use.
- **Streaming I/O** — `DownloadAsync` uses `ResponseHeadersRead` + pooled 80 KB buffers; multipart uploads stream from source; JSON serialization uses `JsonContent` (streamed, not string-materialized).
- **Bounded memory** — error capture caps (4 KB / 1 MB hard read cap), cache entry-size limit (1 MiB), pagination page-at-a-time enumeration.
- **Telemetry that costs nothing when off** — `ActivitySource`/`Meter` no-op without listeners; logging via source-generated `LoggerMessage` (no boxing/formatting when the level is off).
- **Allocation-conscious hot path** — `TagList` (stack-based) for metric tags, pooled copy buffers, no LINQ in `SendAsync`'s critical section.

## Tuning knobs

| Goal | Knob |
|---|---|
| High-throughput bursts to one host | raise `SocketsHttpHandler` connection limit via `builder.HttpClientBuilder.ConfigurePrimaryHttpMessageHandler` |
| Long downloads | `WithTimeout` per request + raise resilience `AttemptTimeout`/`TotalRequestTimeout` |
| Chatty reference data | `WithCache` / `WithResponseCaching` — cache hits skip the entire pipeline |
| Reduce log overhead | `System.Net.Http`/`Polly` categories to Warning |
| Concurrency ceilings | resilience rate limiter via `ConfigureResilience` |

## Concurrency model

`IApiClient` is thread-safe; fan out with `Task.WhenAll` freely — the resilience pipeline's rate limiter and the circuit breaker provide backpressure. Builders are per-request objects; never share one across tasks.

## Known limitations

Response caching rehydrates bodies as `byte[]` (by design, entry-size-capped); pagination is sequential per enumeration; no HTTP/3-specific tuning surface (available via the primary-handler escape hatch).
