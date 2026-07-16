# Recipes — Production Configuration

A battle-ready registration combining the recommended settings:

```csharp
services.AddKorasApiClient("partner", configuration.GetSection("Clients:Partner"), options =>
{
    options.DefaultHeaders["Accept"] = "application/json";
    options.MaxErrorResponseBodyLength = 1024;
})
.WithClientCredentials(oauth =>
{
    oauth.TokenEndpoint = new Uri(configuration["Partner:TokenEndpoint"]!);
    oauth.ClientId = configuration["Partner:ClientId"]!;
    oauth.ClientSecret = configuration["Partner:ClientSecret"]!;   // from Key Vault-backed configuration
    oauth.Scopes.Add("orders:read");
})
.ConfigureResilience(resilience =>
{
    resilience.Retry.MaxRetryAttempts = 4;
    resilience.CircuitBreaker.FailureRatio = 0.2;
    resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
})
.AddTypedClient<PartnerGateway>();

services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource(KorasApiClientTelemetry.ActivitySourceName).AddHttpClientInstrumentation())
    .WithMetrics(m => m.AddMeter(KorasApiClientTelemetry.MeterName));
```

```json
{
  "Clients": { "Partner": { "BaseAddress": "https://api.partner.com/", "Timeout": "00:00:45" } },
  "Logging": { "LogLevel": { "System.Net.Http": "Warning", "Polly": "Warning", "Koras.ApiClient": "Information" } }
}
```

Checklist: secrets from a vault ✅ · startup validation on ✅ · timeouts aligned (options + resilience) ✅ · noisy log categories damped ✅ · per-dependency dashboards from `koras.apiclient.*` metrics ✅ · alerts on circuit-breaker opens via Polly telemetry ✅.
