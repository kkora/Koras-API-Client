# ADR-0003: Errors as data (ApiResponse/ApiError), exceptions opt-in

**Status:** Accepted · 2026-07-16

## Context

HTTP client libraries choose between throwing on non-success (`EnsureSuccessStatusCode`), returning raw status codes, or returning result objects. Enterprise integration code needs to branch on failure *kinds* (retryable? validation? auth?) without exception-driven control flow, while still allowing exception style at app edges.

## Decision

`SendAsync` returns `ApiResponse`/`ApiResponse<T>` carrying either a value or a categorized `ApiError`; it never throws for transport/HTTP/deserialization failures. `EnsureSuccess()`/`GetValueOrThrow()` convert to a single `ApiClientException` for callers who prefer exceptions. Caller cancellation always surfaces as `OperationCanceledException` (platform contract).

## Alternatives considered

- **Throw-by-default** — hostile to resilience/fallback logic; forces catch-and-inspect patterns; loses error taxonomy.
- **Full functional Result<T,E> library dependency** — pulls an opinionated dependency into Abstractions; overkill for the two-outcome shape.

## Consequences

- Every failure path must be normalized in one place (`ApiErrorFactory`) with a tested mapping table.
- `ApiResponse<T>.Value` is nullable; the docs and `GetValueOrThrow()` steer safe consumption.
- Timeout vs. caller-cancellation must be carefully distinguished (tested explicitly).
