# Performance Testing

## Benchmarks

`benchmarks/Koras.ApiClient.Benchmarks` (BenchmarkDotNet, `[MemoryDiagnoser]`) covers the hot paths over an in-memory transport so numbers reflect framework overhead, not network noise:

- **Send + deserialize (success)** — full logical pipeline: URI composition, correlation, telemetry, `System.Text.Json` deserialization
- **Send → 404 (error normalization)** — bounded body capture + problem parsing + category mapping
- **Request building only** — fluent builder allocation profile

```bash
dotnet run -c Release --project benchmarks/Koras.ApiClient.Benchmarks -- --filter *SendBenchmarks*
```

## Methodology

- Run on Release builds, quiet machine, default BDN job (warmup + pilot + 15 iterations).
- Compare before/after for any PR touching `KorasApiClient.SendAsync`, `ApiErrorFactory`, or `BuildRequestUri` (CLAUDE.md rule).
- Track allocations first, mean time second — the framework's overhead budget is "negligible next to a network call" (< a few µs / < a few KB per request).

## Regression policy

No CI gate yet (benchmark runners are too noisy on shared agents); results are attached to PRs that touch hot paths, and significant regressions (>20 % time or allocation on the send path) block merge per review policy. Publishing a tracked baseline table in [performance/benchmarks.md](../performance/benchmarks.md) is part of the 1.0 checklist.
