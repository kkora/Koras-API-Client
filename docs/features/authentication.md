# Authentication

## Overview

Three schemes ship in the box — API key, bearer tokens via `IAccessTokenProvider`, and OAuth 2.0 client credentials — all as `DelegatingHandler`s on the `HttpClientFactory` pipeline, all configured with one builder call. Custom schemes plug into the same seams.

## API key

```csharp
services.AddKorasApiClient("vendor", o => o.BaseAddress = new Uri("https://api.vendor.com/"))
    .WithApiKey(key =>
    {
        key.ApiKey = configuration["Vendor:ApiKey"]!;   // from user-secrets/env/key vault — never hard-coded
        key.HeaderName = "X-Api-Key";                    // default
        // key.Location = ApiKeyLocation.QueryString;    // explicit opt-in only: URLs get logged by proxies
    });
```

Missing keys fail at **startup** (options validation), not on the first request.

## Bearer tokens (any source)

```csharp
public sealed class MyStsProvider : IAccessTokenProvider
{
    public ValueTask<AccessToken> GetTokenAsync(bool forceRefresh, CancellationToken ct) => ...;
}
builder.WithBearerToken<MyStsProvider>();                       // resolved from DI, singleton
builder.WithBearerToken(sp => new DelegateTokenProvider(...));  // factory form
builder.WithStaticBearerToken("dev-token");                     // development/testing only
```

**401 behavior:** when a *bodiless* request gets 401, the handler force-refreshes the token and retries exactly once. Requests with content are never replayed (their streams may be consumed) — the 401 surfaces as `ApiErrorCategory.Authentication`.

## OAuth 2.0 client credentials

```csharp
builder.WithClientCredentials(oauth =>
{
    oauth.TokenEndpoint = new Uri("https://login.partner.com/oauth/token");
    oauth.ClientId = configuration["Partner:ClientId"]!;
    oauth.ClientSecret = configuration["Partner:ClientSecret"]!;
    oauth.Scopes.Add("read:orders");
    oauth.RefreshSkew = TimeSpan.FromSeconds(60);   // refresh 60 s before true expiry (default)
});
```

Lifecycle: tokens are cached until `expires_in − RefreshSkew`; concurrent callers share one refresh (semaphore single-flight); the token endpoint is called over its own named, resilient `HttpClient`. Endpoint failures surface as `Authentication`-category errors on the *outbound* call — your code handles one error model.

## Managed identity (Azure) — via the provider seam

The core deliberately has no Azure dependency ([ADR-0005](../architecture/decision-records/adr-0005-no-azure-dependency-in-core.md)). Adapter until `Koras.ApiClient.Azure` ships:

```csharp
builder.WithBearerToken(sp =>
{
    var credential = new Azure.Identity.DefaultAzureCredential();
    return new DelegateTokenProvider(async (_, ct) =>
    {
        var token = await credential.GetTokenAsync(
            new Azure.Core.TokenRequestContext(["https://your-api/.default"]), ct);
        return new AccessToken(token.Token, token.ExpiresOn);
    });
});
```

## Security considerations

Tokens/keys are never logged by the framework (path-only URL logging, no credential headers in logs). Secrets belong in configuration providers or secret stores. Query-string keys require explicit opt-in and are excluded from framework logs, but remote servers will still see and log them.

## Thread safety

All providers and handlers are thread-safe; providers register as singletons per named client.

## Testing

Chain handlers over `MockApiHandler` and assert on `Requests[i].Headers["Authorization"]`; script token endpoints with `RespondWithJson(new { access_token = "...", expires_in = 3600 })`. See `AuthenticationTests` and `ClientCredentialsTests`.

## Common mistakes

- Hard-coding secrets in `Program.cs` (use user-secrets/environment/vault).
- Expecting 401-retry on POSTs — bodied requests intentionally never replay.
- One token provider across differently-scoped clients — register per named client.
