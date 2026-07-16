# Troubleshooting — Common Errors

## `InvalidOperationException: An invalid request URI was provided…`

Relative path with no `BaseAddress`. Set `options.BaseAddress` (absolute, trailing slash) or use an absolute URL in the verb call.

## My path's `/v1/` prefix disappears

`client.Get("/orders")` (leading slash) resolves against the **host root**. Use relative `"orders"` with `BaseAddress = https://host/v1/` (trailing slash matters — it's standard `HttpClient` URI combination).

## `OptionsValidationException` at startup

Working as intended — read the message; rules in [configuration/validation.md](../configuration/validation.md).

## Everything returns `Timeout` after ~30 s even though I raised `options.Timeout`

The resilience pipeline's own `TotalRequestTimeout` (30 s default) fires first. Raise both — see [features/resilience.md](../features/resilience.md).

## `ApiErrorCategory.Serialization` / `deserialization_failed`

Response body didn't match `T`: wrong casing (client defaults are camelCase-insensitive — usually fine), API returning HTML error pages with 200, or a mismatched shape. Inspect with `SendAsync<string>` or check `error.Exception`.

## 401 loop / token never refreshes

`WithStaticBearerToken` never refreshes (by design). For real flows use `WithClientCredentials` or your own `IAccessTokenProvider`; remember 401-retry only applies to bodiless requests.

## Cache "not working"

Caching applies to GET + HTTP 200 only, requires `WithCache(...)` per request or `WithResponseCaching()` per client, and skips bodies over `MaxEntrySizeBytes`.

## `IApiClient` not resolvable from DI

The injectable `IApiClient` exists only via the default (`AddKorasApiClient(configure)`) overload. With named clients, inject `IApiClientFactory` or use `AddTypedClient<T>`.

## Duplicated retries / 9 attempts

You wrapped `SendAsync` in your own retry loop on top of the built-in 3 retries. Tune `ConfigureResilience` instead, or branch on `error.IsTransient` for app-level fallback (not blind retry).

## `MockApiHandler` returns 404 for everything

Pattern mismatch: patterns match the absolute path (`/v1/orders` when your base address has `/v1/`), include `?` only for exact path-and-query matches, end with `*` for prefixes. Set `ThrowOnUnmatchedRequest = true` to see the unmatched URI in the exception.
