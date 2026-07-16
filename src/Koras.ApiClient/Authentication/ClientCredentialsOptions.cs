namespace Koras.ApiClient.Authentication;

/// <summary>Configuration for the OAuth 2.0 client-credentials grant on a named client.</summary>
public sealed class ClientCredentialsOptions
{
    /// <summary>The absolute URL of the OAuth token endpoint.</summary>
    public Uri? TokenEndpoint { get; set; }

    /// <summary>The OAuth client identifier.</summary>
    public string? ClientId { get; set; }

    /// <summary>The OAuth client secret. Source it from configuration or a secret store — never hard-code it.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>The scopes requested, joined with spaces into the <c>scope</c> parameter.</summary>
    public IList<string> Scopes { get; } = [];

    /// <summary>Additional form parameters sent with the token request (for example <c>audience</c>).</summary>
    public IDictionary<string, string> AdditionalParameters { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// How long before its true expiry a cached token is considered stale and refreshed.
    /// Defaults to 60 seconds.
    /// </summary>
    public TimeSpan RefreshSkew { get; set; } = TimeSpan.FromSeconds(60);
}
