# Diagnostics

## Turn up the logs

```json
{ "Logging": { "LogLevel": {
    "Koras.ApiClient": "Debug",          // request-starting lines + completions
    "System.Net.Http": "Information",    // per-attempt HttpClient logs
    "Polly": "Information"               // retry/circuit-breaker decisions
} } }
```

`Koras.ApiClient` event IDs: 1 starting · 2 completed · 3 non-success status · 4 transport failure. Every line carries the correlation ID — grep it across services.

## Follow one request across the fleet

`ApiResponse.CorrelationId` ↔ `X-Correlation-ID` header ↔ log lines ↔ span tag `koras.correlation_id`. Have downstream services echo/propagate the header and the trail is complete.

## Inspect what actually failed

`ApiError` is the black box recorder: `Category`, `Code`, `StatusCode`, `Problem` (parsed RFC 9457), `ResponseBody` (bounded raw capture), `RetryAfter`, `Exception`. Log it structurally in your handler:

```csharp
logger.LogWarning("Vendor call failed: {Category} {Code} {Status} {Body}",
    error.Category, error.Code, error.StatusCode, error.ResponseBody);
```

## Live counters & traces

```bash
dotnet-counters monitor --process-id <pid> Koras.ApiClient System.Net.Http
```

…or wire OpenTelemetry ([features/observability.md](../features/observability.md)) and look at the `GET <client>` spans — retries appear as child `System.Net.Http` attempt spans, which instantly distinguishes "slow API" from "retry storm".

## Reproduce it in a test

The fastest diagnosis is often a 10-line repro with `MockApiHandler` scripting the suspicious response (`RespondWith(status, body)` with the captured `ResponseBody`) — see [testing-support.md](../features/testing-support.md).
