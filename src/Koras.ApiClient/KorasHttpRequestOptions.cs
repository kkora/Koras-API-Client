namespace Koras.ApiClient;

/// <summary>
/// Well-known <see cref="HttpRequestMessage.Options"/> keys used to flow per-request settings from
/// <see cref="KorasApiClient"/> into the handler pipeline. Exposed for custom handler authors.
/// </summary>
public static class KorasHttpRequestOptions
{
    /// <summary>Carries <see cref="ApiRequest.CacheDuration"/> to the response caching handler.</summary>
    public static readonly HttpRequestOptionsKey<TimeSpan?> CacheDuration = new("Koras.ApiClient.CacheDuration");
}
