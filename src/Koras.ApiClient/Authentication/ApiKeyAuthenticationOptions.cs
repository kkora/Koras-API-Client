namespace Koras.ApiClient.Authentication;

/// <summary>Where an API key is attached to outgoing requests.</summary>
public enum ApiKeyLocation
{
    /// <summary>Send the key as a request header (default, recommended — headers do not leak into URL logs).</summary>
    Header = 0,

    /// <summary>
    /// Send the key as a query-string parameter. Only use when the API requires it: URLs are
    /// commonly logged by proxies and servers.
    /// </summary>
    QueryString = 1,
}

/// <summary>Configuration for API-key authentication on a named client.</summary>
public sealed class ApiKeyAuthenticationOptions
{
    /// <summary>The API key value. Source it from configuration or a secret store — never hard-code it.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The header carrying the key when <see cref="Location"/> is <see cref="ApiKeyLocation.Header"/>. Defaults to <c>X-Api-Key</c>.</summary>
    public string HeaderName { get; set; } = "X-Api-Key";

    /// <summary>Where the key is attached. Defaults to <see cref="ApiKeyLocation.Header"/>.</summary>
    public ApiKeyLocation Location { get; set; } = ApiKeyLocation.Header;

    /// <summary>The query parameter carrying the key when <see cref="Location"/> is <see cref="ApiKeyLocation.QueryString"/>. Defaults to <c>api_key</c>.</summary>
    public string QueryParameterName { get; set; } = "api_key";
}
