# Options Validation

All Koras options register with `ValidateOnStart` — a misconfigured client stops the host at boot with a precise message instead of failing on the first 2 a.m. request.

## What is validated

| Options | Rules |
|---|---|
| `KorasApiClientOptions` | `Timeout > 0`; `BaseAddress` absolute when set; correlation header name non-blank when correlation enabled; `MaxErrorResponseBodyLength ≥ 0`; caching duration > 0 when enabled; `MaxEntrySizeBytes ≥ 0` |
| `ApiKeyAuthenticationOptions` | `ApiKey` non-empty |
| `ClientCredentialsOptions` | `TokenEndpoint`, `ClientId`, `ClientSecret` all present |

## Failure shape

```text
Microsoft.Extensions.Options.OptionsValidationException:
  Timeout must be positive.
```

Thrown at host start (Generic Host / WebApplication both run `IStartupValidator`), or on first `IOptionsMonitor.Get(name)` in bare `ServiceCollection` scenarios — which is exactly what the DI unit tests assert.

## Custom validation

Add your own on the same named options:

```csharp
services.AddOptions<KorasApiClientOptions>("vendor")
    .Validate(o => o.BaseAddress!.Host.EndsWith("vendor.com"), "vendor client must point at vendor.com");
```
