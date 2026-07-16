# Installation

## Packages

| Package | Install in | Purpose |
|---|---|---|
| `Koras.ApiClient` | applications | the framework (brings Abstractions transitively) |
| `Koras.ApiClient.Abstractions` | SDKs / domain assemblies | contracts only, zero third-party deps |
| `Koras.ApiClient.Testing` | test projects | `MockApiHandler` + test factory |

```bash
dotnet add package Koras.ApiClient
dotnet add package Koras.ApiClient.Testing   # test projects
```

## Requirements

- .NET 8, .NET 9, or .NET 10 (no .NET Framework / netstandard2.0 — see [ADR-0002](../architecture/decision-records/adr-0002-target-frameworks.md))
- Dependencies: `Microsoft.Extensions.Http`, `Microsoft.Extensions.Http.Resilience` (Polly v8 transitively), `Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.Options.ConfigurationExtensions` — all MIT, all Microsoft-maintained.

## Verify

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddKorasApiClient(o => o.BaseAddress = new Uri("https://api.github.com/"));
```

Compiles? You're installed. Continue with the [quick start](quick-start.md).
