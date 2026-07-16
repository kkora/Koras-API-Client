# File Upload & Download

## Overview

Multipart uploads stream directly from source streams, and downloads stream directly into destination streams with progress reporting — large payloads never buffer fully in memory.

## Upload (multipart/form-data)

```csharp
await using var file = File.OpenRead(path);
var response = await client.Post("documents")
    .WithMultipart(m => m
        .AddField("category", "invoices")
        .AddFile("file", file, Path.GetFileName(path), "application/pdf"))
    .SendAsync<DocumentCreated>(ct);
```

`AddFile` wraps the stream in `StreamContent` — it is read while sending, not preloaded. The stream is disposed with the request content.

## Download (streaming with progress)

```csharp
await using var destination = File.Create(targetPath);
var progress = new Progress<long>(bytes => Console.Write($"\r{bytes:N0} bytes"));

var response = await client.Get($"documents/{id}/content")
    .DownloadAsync(destination, progress, ct);
response.EnsureSuccess();
```

`DownloadAsync` uses `ResponseHeadersRead` and copies through a pooled 80 KB buffer — an N-gigabyte body costs ~80 KB of working memory.

## Error handling

- Non-success statuses return the usual categorized `ApiError` (body captured bounded) without touching `destination`.
- Mid-copy connection failures return `Network`-category errors; the destination may hold a partial body — write to a temp file and move on success:

```csharp
var temp = Path.GetTempFileName();
await using (var stream = File.Create(temp))
{
    (await client.Get(url).DownloadAsync(stream, null, ct)).EnsureSuccess();
}
File.Move(temp, finalPath, overwrite: true);
```

## Cancellation

Both directions honor the token immediately, including mid-copy.

## Performance considerations

Progress callbacks fire per buffer (~80 KB); keep them cheap or throttle UI updates. For upload progress, wrap your source stream in a counting stream.

## Testing

`DownloadAndMultipartTests` show the pattern: `MockApiHandler` records the multipart body (boundary, filenames, field values) and serves large bodies for download assertions.

## Common mistakes

- Buffering downloads via `SendAsync<byte[]>` — that's not supported by design; use `DownloadAsync` and streams.
- Reusing a consumed upload stream for a retry — build a fresh request (and fresh stream) per attempt.
