# Secure Configuration

## Credentials

- Source `ApiKey`, `ClientSecret`, and static tokens from **user-secrets (dev)**, **environment variables**, or a **secret store** (Key Vault, AWS Secrets Manager) — never from committed `appsettings.json` and never hard-coded.
- Prefer header-based API keys (default). `ApiKeyLocation.QueryString` only when the API mandates it — remote servers and proxies log URLs.
- Prefer `WithClientCredentials` or a real `IAccessTokenProvider` over `WithStaticBearerToken` (dev/test helper).

## Safe defaults you keep by not touching anything

| Concern | Default |
|---|---|
| TLS validation | BCL defaults, always on — no bypass switch exists in this framework |
| Retries | bounded (3) + circuit breaker + timeouts |
| Logs | path-only URLs, no credential headers |
| Error body capture | bounded 4 KB (1 MB read cap) |
| Caching | off until opted in |
| Pagination | 1000-page bound |

## Hardening checklist for production clients

```csharp
services.AddKorasApiClient("partner", configuration.GetSection("Clients:Partner"), options =>
{
    options.MaxErrorResponseBodyLength = 1024;          // or 0 / CaptureErrorResponseBody = false for sensitive APIs
    options.UserAgent = "my-service/1.4";               // identifiable, no versions you don't want disclosed
})
.WithClientCredentials(o => { /* secrets from config */ })
.ConfigureResilience(r => r.CircuitBreaker.FailureRatio = 0.2);
```

- [ ] Secrets from a secret store; nothing sensitive in the repo
- [ ] `ValidateOnStart` reachable (host runs `IStartupValidator`) — true for Generic Host / WebApplication
- [ ] Error-body capture reduced/disabled for APIs that echo payloads
- [ ] No response caching on per-user endpoints
- [ ] Log levels: `System.Net.Http`/`Polly` at Warning in production
- [ ] Outbound allowlisting at the network layer for high-trust environments (SSRF defense in depth)
