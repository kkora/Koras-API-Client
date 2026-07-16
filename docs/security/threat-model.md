# Threat Model

## System context & trust boundaries

```
[App code]  --trusted-->  [Koras.ApiClient in-proc]  --UNTRUSTED-->  [Remote APIs / network]
     |                            |
     +--semi-trusted--> [Configuration / secret stores]  [Logs & telemetry sinks]
```

The framework runs in the application's trust domain. Untrusted inputs: **every byte of every HTTP response** (bodies, headers, status lines) and, partially, configuration values. Sensitive assets: API keys, client secrets, bearer tokens, request/response payloads.

## Threats & mitigations (STRIDE-flavored)

| # | Threat | Mitigation |
|---|---|---|
| T1 | **Credential disclosure via logs/telemetry** | Path-only URL logging; `Authorization`/API-key headers never logged; token values never logged; verified by `Logging_NeverIncludesQueryStringsOrCredentials` |
| T2 | **Credential disclosure via URLs** | Header placement is the API-key default; query placement is explicit opt-in with doc warnings |
| T3 | **Malicious response bodies (deserialization)** | `System.Text.Json` only (no polymorphic/type-name handling, no `BinaryFormatter`-class risks); parse failures normalize to `Serialization` errors |
| T4 | **Memory exhaustion via huge error bodies** | bounded capture (4 KB default, 1 MB hard read cap, disabled-able); downloads stream with 80 KB pooled buffers |
| T5 | **Hostile pagination (infinite cursors)** | `maxPages` guard (default 1000) throws instead of looping |
| T6 | **Retry amplification / self-DoS during outages** | bounded standard pipeline: 3 retries + circuit breaker + total timeout; `Retry-After` honored |
| T7 | **Cache poisoning / cross-tenant leakage** | caching is opt-in; keys include client name + full URI; only HTTP 200 cached; per-user-variant endpoints documented as do-not-cache (R4) |
| T8 | **SSRF via attacker-influenced paths** | base-address anchoring is the norm; absolute-URL requests are an explicit developer act; consumers composing paths from user input must validate (documented; the framework never follows cross-origin redirects with credentials silently — BCL default redirect handling applies) |
| T9 | **Token replay of request bodies** | 401 refresh-retry only for bodiless requests; bodied requests never replayed |
| T10 | **Supply-chain compromise** | Microsoft-only dependency graph, CPM pinning, Dependabot, CodeQL, dependency review, `dotnet list package --vulnerable` gate, deterministic builds + Source Link |

## Assumptions

TLS is provided by `SocketsHttpHandler` defaults (certificate validation **on**; the framework offers no switch to disable it). Secrets arrive via the options pattern from the host's configuration system; their at-rest protection is the host's responsibility. Multi-tenant isolation of clients is per named client — tenant-specific credentials require tenant-specific named clients or per-request headers.

## Out of scope

Vulnerabilities in remote APIs; host-level compromise (an attacker in-process owns the secrets regardless); network-layer attacks below TLS.
