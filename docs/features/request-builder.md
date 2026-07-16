# Fluent Request Builder

## Overview

The request builder composes and sends HTTP requests fluently: `client.Get("orders").WithQuery("status", "open").SendAsync<OrderList>(ct)`. Nothing touches the network until a terminal method (`SendAsync`, `SendAsync<T>`, `DownloadAsync`) runs.

## When to use it / when not

Use it for all hand-written REST integration. If you need full control of `HttpRequestMessage` (custom versions, exotic content), use `WithContent(HttpContent)` or drop to the raw `IHttpClientBuilder` seam.

## Installation & required packages

`dotnet add package Koras.ApiClient` (the builder lives in the core package; `ApiRequest`/`ApiResponse` come from `Koras.ApiClient.Abstractions` transitively).

## Basic usage

```csharp
// Verbs: Get, Post, Put, Patch, Delete, Head
var response = await client.Get("orders")
    .WithQuery("status", "open")        // escaped automatically; null values are omitted
    .WithQuery("page", 2)               // invariant-culture formatting for non-strings
    .WithHeader("X-Tenant", tenantId)   // overrides client default headers
    .WithTimeout(TimeSpan.FromSeconds(5))
    .WithCorrelationId(requestId)       // otherwise auto-generated
    .SendAsync<OrderList>(cancellationToken);
```

Bodies:

```csharp
await client.Post("orders").WithJsonBody(newOrder).SendAsync<OrderCreated>(ct);      // client's JsonOptions
await client.Post("login").WithFormUrlEncoded([new("user", u), new("pass", p)]).SendAsync(ct);
await client.Post("docs").WithMultipart(m => m.AddField("kind", "invoice")
                                              .AddFile("file", stream, "invoice.pdf", "application/pdf")).SendAsync(ct);
await client.Put("blob").WithContent(new StreamContent(stream)).SendAsync(ct);       // escape hatch
```

Convenience one-liners: `client.GetFromJsonAsync<T>(path, ct)`, `client.PostJsonAsync<TReq,TRes>(path, body, ct)`.

## Public API

`ApiRequestBuilder` (all `With*` return the builder), `MultipartBuilder`, `ApiClientExtensions` verbs, `ApiRequest` (`Build()` exposes the raw data object).

## Execution lifecycle

Builder → `ApiRequest` (pure data) → `KorasApiClient` composes the URI (query escaping, path?query merging), applies default headers + user agent + correlation, starts the telemetry span, sends through the handler pipeline (caching → resilience → auth), then normalizes the outcome. See [architecture/diagrams.md](../architecture/diagrams.md).

## Errors, cancellation, timeouts

All runtime failures return `ApiError` (see [error-handling.md](error-handling.md)). `WithTimeout` produces `Timeout`-category errors; caller cancellation throws `OperationCanceledException`. Argument mistakes (empty query name, non-positive timeout) throw immediately at build time.

## Thread safety

Builders are single-use and not thread-safe; clients are thread-safe. Build, send, discard.

## Testing

Pair with `MockApiHandler` — every recorded request exposes the final URI, headers, and body ([testing-support.md](testing-support.md)).

## Common mistakes

- Reusing one builder for multiple sends with a body — content streams can only be consumed once.
- Leading `/` in paths: `client.Get("/orders")` resolves against the host root, ignoring a base-address path segment like `/v1/`. Prefer relative paths (`"orders"`) with a trailing-slash base address.
- Putting secrets in query strings — use headers (`WithApiKey`) unless the API demands otherwise.
