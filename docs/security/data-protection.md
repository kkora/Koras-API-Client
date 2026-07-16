# Data Protection

## Data the framework holds, and for how long

| Data | Where | Lifetime | Protection |
|---|---|---|---|
| API keys / client secrets | options instances (process memory) | process | never logged, never serialized by the framework |
| Bearer tokens | provider caches (process memory) | until expiry−skew | never logged; single-flight refresh avoids fan-out |
| Request/response bodies | transient buffers | per request | streamed for downloads/uploads; error capture bounded |
| Cached GET responses | `IMemoryCache` | configured TTL | in-process only; opt-in; size-bounded |
| Correlation IDs | logs/traces/headers | sink retention | random GUIDs — no PII by construction |

## PII guidance for consumers

The framework treats payloads as opaque. If your API payloads contain PII:

- Disable or shrink error-body capture (`CaptureErrorResponseBody`/`MaxErrorResponseBodyLength`) so PII cannot land in `ApiError.ResponseBody` that *your* code might log.
- Don't cache PII-bearing GETs, or key the URI per principal.
- Scrub PII in your own log enrichment — framework logs contain method, path, status, timing, correlation ID only.

## Encryption expectations

In transit: HTTPS/TLS via the BCL; the framework adds no downgrade paths. At rest: the framework persists nothing to disk — no temp files, no offline queues (the roadmap's offline queue will ship with its own encryption story).

## Multi-tenant guidance

Isolate tenants by **named client** (per-tenant credentials/options) or per-request headers. The response cache keys by client name + URI — tenant-variant responses behind identical URIs on one shared client must not enable caching.
