# Configuration

## Code, config file, or both

```csharp
// pure code
services.AddKorasApiClient("vendor", o => { o.BaseAddress = new Uri("https://api.vendor.com/"); });

// bound from appsettings.json, refined in code (code wins)
services.AddKorasApiClient("vendor", configuration.GetSection("Clients:Vendor"),
    o => o.DefaultHeaders["X-Client"] = "billing");
```

```json
{
  "Clients": {
    "Vendor": {
      "BaseAddress": "https://api.vendor.com/",
      "Timeout": "00:00:20",
      "UserAgent": "billing-service/2.1",
      "EnableCorrelationId": true,
      "CorrelationHeaderName": "X-Request-ID",
      "CaptureErrorResponseBody": true,
      "MaxErrorResponseBodyLength": 2048,
      "Caching": { "Enabled": true, "DefaultDuration": "00:01:00", "MaxEntrySizeBytes": 524288 }
    }
  }
}
```

The full option reference lives in [configuration/all-options.md](../configuration/all-options.md).

## Secrets

Keys and client secrets are **not** options-file material in plaintext repos. Use:

```bash
dotnet user-secrets set "Vendor:ApiKey" "…"          # development
```

…and environment variables / Key Vault / AWS Secrets Manager in production, then reference them:

```csharp
.WithApiKey(k => k.ApiKey = configuration["Vendor:ApiKey"]!);
```

## Validation

All options validate at startup (`ValidateOnStart`): bad timeout, relative base address, enabled-but-invalid caching, missing API key or client-credential fields → the host fails fast with a precise message. See [configuration/validation.md](../configuration/validation.md).
