# Error Handling

## Overview

Every failure — connection errors, timeouts, non-success statuses, deserialization problems — becomes a categorized `ApiError` on the `ApiResponse`. `SendAsync` never throws for runtime failures; exceptions are opt-in.

## Basic usage

```csharp
var response = await client.Get($"orders/{id}").SendAsync<Order>(ct);

// Style 1 — branch on data
if (response.Error is { } error)
{
    if (error.Category == ApiErrorCategory.NotFound) return null;
    if (error.IsTransient) return await FallbackAsync(ct);
    logger.LogError("Order lookup failed: {Code} {Message}", error.Code, error.Message);
    return null;
}
var order = response.Value!;

// Style 2 — functional
return response.Match(order => Map(order!), error => throw new DomainException(error.Message));

// Style 3 — exceptions at the edge
try { return response.GetValueOrThrow(); }
catch (ApiClientException ex) { /* single exception type; ex.Error carries everything */ }
```

## The taxonomy

See the full mapping table in [architecture/error-model.md](../architecture/error-model.md). Highlights:

- `error.Category` — switchable coarse kind (Network, Timeout, Validation, Authentication, …)
- `error.Code` — stable machine string (`http_404`, `network_error`, `timeout`, `deserialization_failed`, `token_request_failed`)
- `error.Problem` — parsed RFC 9457 ProblemDetails (`Title`, `Detail`, `Errors` dictionary, extension members) when the API returned one
- `error.RetryAfter` — parsed `Retry-After` on 429/503
- `error.IsTransient` — whether an app-level retry could plausibly help
- `error.ResponseBody` — bounded raw-body capture for diagnostics (default 4 KB via `MaxErrorResponseBodyLength`; disable with `CaptureErrorResponseBody = false`)
- `error.Exception` — the underlying exception for transport/timeout/serialization failures

## What still throws

| Exception | Meaning |
|---|---|
| `OperationCanceledException` | *your* token was cancelled |
| `ArgumentNullException` / `ArgumentException` / `InvalidOperationException` | programmer error (null request, relative path without base address) |
| `ApiClientException` | only when *you* call `EnsureSuccess()` / `GetValueOrThrow()`, or a pagination page fails |

## ASP.NET Core mapping

```csharp
return response.Match<IResult>(
    value => Results.Ok(value),
    error => error.Category switch
    {
        ApiErrorCategory.NotFound => Results.NotFound(),
        ApiErrorCategory.Validation => Results.ValidationProblem(error.Problem?.Errors ?? new Dictionary<string, string[]>()),
        _ when error.IsTransient => Results.Problem(statusCode: 503, title: "Upstream unavailable"),
        _ => Results.Problem(statusCode: 502, title: error.Message),
    });
```

## Security considerations

`ApiError.Message` never contains credentials; body capture is bounded; disable capture entirely for endpoints that may echo sensitive payloads.

## Testing

`MockApiHandler.RespondWithProblem(...)`, `.RespondWith(status)`, `.Throws(new HttpRequestException())` reproduce every category — see the mapping tests in `tests/Koras.ApiClient.UnitTests/ErrorMappingTests.cs`.

## Common mistakes

- Checking `StatusCode` instead of `Error`/`Category` — transport failures have no status code.
- Treating `Value` as non-null on success: HTTP 204 succeeds with `Value == null`; use `GetValueOrThrow()` when a body is mandatory.
