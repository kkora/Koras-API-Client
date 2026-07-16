# Observability (Logging, Tracing, Metrics, Correlation)

## Overview

Every logical operation emits: structured logs (category `Koras.ApiClient`), one `ActivitySource` span, `Meter` measurements, and a correlation ID header — all with **secret-free** guarantees. No OpenTelemetry dependency in the package; instruments are BCL types that cost nothing without listeners.

## OpenTelemetry wiring

```csharp
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(KorasApiClientTelemetry.ActivitySourceName)   // "Koras.ApiClient"
        .AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics
        .AddMeter(KorasApiClientTelemetry.MeterName));
```

## What you get

**Spans** — name `{METHOD} {clientName}` (e.g. `GET vendor`), kind Client, tags `http.request.method`, `url.path` (query stripped), `koras.client.name`, `koras.correlation_id`, `http.response.status_code`, `error.type`. Retry attempts appear as nested BCL `System.Net.Http` spans.

**Metrics** — `koras.apiclient.requests` (counter) and `koras.apiclient.request.duration` (histogram, ms), tagged with client name, method, status code, and error type.

**Logs** —

```text
info: Koras.ApiClient.KorasApiClient[2]
      GET todos/1 on client Default completed with status 200 in 42.1ms (correlation: 4734aca4…)
warn: Koras.ApiClient.KorasApiClient[3]
      GET todos/999999 on client Default returned non-success status 404 in 0.7ms (correlation: ddb91d…)
```

Event ids: 1 starting (Debug), 2 completed (Information), 3 non-success (Warning), 4 transport failure (Warning).

## Correlation IDs

Enabled by default: header `X-Correlation-ID` (rename via `CorrelationHeaderName`), generated per request when not supplied via `WithCorrelationId(...)`, echoed on `ApiResponse.CorrelationId`, stamped on spans and every log line. Disable with `EnableCorrelationId = false`.

## Redaction guarantees

- URLs are logged **path-only** — query strings never appear (API keys in queries stay out of logs).
- `Authorization`, API-key headers, and token values are never logged at any level.
- Error bodies are captured onto `ApiError` (bounded), not logged by the framework.

Verified by `TelemetryTests.Logging_NeverIncludesQueryStringsOrCredentials`.

## Log-volume tuning

`HttpClientFactory` and Polly add their own informational logs. Production `appsettings.json` typically wants:

```json
{ "Logging": { "LogLevel": { "System.Net.Http": "Warning", "Polly": "Warning" } } }
```

## Testing

Use `ActivityListener`/`MeterListener` (see `TelemetryTests`) or assert correlation propagation with `MockApiHandler.Requests[i].Headers["X-Correlation-ID"]`.
