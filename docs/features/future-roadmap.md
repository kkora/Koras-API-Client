# Feature Roadmap Beyond MVP

| Version | Feature | Notes |
|---|---|---|
| 0.5.0 | `Koras.ApiClient.Azure` | `TokenCredential` → `IAccessTokenProvider` adapter; managed identity, workload identity |
| 0.5.0 | Health-check integration | `AddKorasApiClientHealthCheck(name, probePath)` in a separate package to keep core lean |
| 1.1 | Idempotency-key support | Auto `Idempotency-Key` header for unsafe retried methods, pluggable key source |
| 1.1 | Distributed response caching | `IDistributedCache` backend behind the existing caching seam |
| 1.2 | Request signing | HMAC/AWS-SigV4-style signing extension point (`IRequestSigner`) |
| 1.2 | Bulkhead presets | Named-client concurrency limiter presets over Polly bulkhead |
| 2.0 | OpenAPI interop | Generate typed clients that ride on `IApiClient` |
| Experimental | Offline queue | Durable store-and-forward for occasionally-connected apps |
| Experimental | Contract-testing helpers | Provider stubs from recorded `MockApiHandler` traffic |

Promotion rule: experimental features ship as `Koras.ApiClient.<Feature>` prerelease packages and never destabilize the core public API.
