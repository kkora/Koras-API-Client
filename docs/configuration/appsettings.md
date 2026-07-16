# appsettings.json Examples

## Minimal

```json
{ "Clients": { "Vendor": { "BaseAddress": "https://api.vendor.com/" } } }
```

```csharp
services.AddKorasApiClient("vendor", configuration.GetSection("Clients:Vendor"));
```

## Full production shape

```json
{
  "Clients": {
    "Vendor": {
      "BaseAddress": "https://api.vendor.com/v2/",
      "Timeout": "00:00:20",
      "UserAgent": "billing-service/2.1",
      "CorrelationHeaderName": "X-Request-ID",
      "MaxErrorResponseBodyLength": 1024,
      "DefaultHeaders": { "Accept": "application/json" },
      "Caching": { "Enabled": true, "DefaultDuration": "00:05:00" }
    }
  },
  "Vendor": { "ApiKey": "" },
  "Logging": { "LogLevel": { "System.Net.Http": "Warning", "Polly": "Warning" } }
}
```

Secrets (`Vendor:ApiKey`) stay empty in the committed file — supplied via user-secrets/environment (`Vendor__ApiKey=...`)/vault.

```csharp
services.AddKorasApiClient("vendor", configuration.GetSection("Clients:Vendor"))
    .WithApiKey(k => k.ApiKey = configuration["Vendor:ApiKey"]!);
```

## Environment variables

Standard .NET configuration mapping: `Clients__Vendor__BaseAddress`, `Clients__Vendor__Timeout`, `Vendor__ApiKey` — no framework-specific handling needed.
