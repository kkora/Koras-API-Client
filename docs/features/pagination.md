# Pagination

## Overview

Consume any paginated API as a flat `IAsyncEnumerable<T>` with bounded page counts and cancellation. Two strategies ship: opaque **cursor** and **page number**; both flow through the same client pipeline (auth, retries, telemetry per page).

## Cursor pagination

```csharp
// API shape: GET /orders?cursor=abc -> { "items": [...], "next": "def" }
public sealed record OrderPage(List<Order> Items, string? Next);

await foreach (var order in client.PaginateAsync<OrderPage, Order>(
    (c, cursor) => c.Get("orders").WithQuery("cursor", cursor),   // null cursor = first page (omitted from query)
    page => new Page<Order>(page.Items, page.Next),
    maxPages: 500,
    cancellationToken: ct))
{
    await ProcessAsync(order, ct);
}
```

Enumeration ends when `NextCursor` is null/empty. Any dialect (Link headers, embedded next URLs) maps through the two delegates.

## Page-number pagination

```csharp
// API shape: GET /posts?_page=1&_limit=25 -> [ ... ] (empty page ends)
await foreach (var post in client.PaginateByPageNumberAsync<List<Post>, Post>(
    "posts",
    page => page,
    pageParameterName: "_page",
    configureRequest: r => r.WithQuery("_limit", 25),
    cancellationToken: ct))
{ ... }
```

Stops at the first empty page.

## Guarantees

- **Bounded**: `maxPages` (default 1000) guards against APIs that never terminate; exceeding it throws `InvalidOperationException` rather than looping forever.
- **No silent truncation**: a failed page throws `ApiClientException` mid-enumeration — partial data is never mistaken for complete data.
- **Streaming**: one page in memory at a time; process items as they arrive.
- **Cancellable**: the token is honored between and during page requests (also via `WithCancellation`).

## Error handling

```csharp
try
{
    await foreach (var item in client.PaginateAsync(...)) { ... }
}
catch (ApiClientException ex) when (ex.Error.IsTransient)
{
    // resume/retry the whole enumeration at app level
}
```

## Performance

Pages fetch sequentially (cursor APIs are inherently sequential). For huge page-number collections needing parallelism, fan out explicit page requests with the request builder instead.

## Testing

Script page sequences with `MockApiHandler` chained `RespondWithJson` calls — see `PaginationTests` for cursor walks, maxPages, mid-stream failures, and cancellation patterns.

## Common mistakes

- `maxPages: int.MaxValue` "to be safe" — pick a real upper bound; the guard exists to catch cursor bugs.
- Forgetting the API's total count: expose it via `Page<T>.TotalCount` if you need progress reporting.
