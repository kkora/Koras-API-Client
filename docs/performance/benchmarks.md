# Benchmarks

## Running

```bash
dotnet run -c Release --project benchmarks/Koras.ApiClient.Benchmarks
# or a subset:
dotnet run -c Release --project benchmarks/Koras.ApiClient.Benchmarks -- --filter "*SendBenchmarks*"
```

BenchmarkDotNet with `[MemoryDiagnoser]`; results land in `BenchmarkDotNet.Artifacts/` (git-ignored).

## What is measured

| Benchmark | Path exercised |
|---|---|
| `GET + JSON deserialize (success path)` | URI composition → correlation → telemetry → mock transport → STJ deserialize → response construction |
| `GET → 404 error normalization` | bounded body capture → problem parse attempt → category mapping → `ApiError` |
| `Request building only (no send)` | fluent builder + `ApiRequest` allocation profile |

The transport is `MockApiHandler` (in-memory), so numbers isolate framework overhead from network noise.

## Baseline policy

A reference table (hardware-stamped) is published here at 1.0.0 and refreshed per minor release. Until then, use relative before/after runs on the same machine when touching hot paths — the review bar is: no >20 % regression in mean time or allocations on the send path without an ADR-documented reason.

## Methodology notes

Release build, single quiet machine, default BDN job; ignore first-run JIT columns; compare `Mean`, `Allocated`. Never benchmark Debug builds (BDN refuses, correctly).
