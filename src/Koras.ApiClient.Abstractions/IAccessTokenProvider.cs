namespace Koras.ApiClient;

/// <summary>
/// Supplies bearer tokens for authenticated requests. This is the extension seam for any token
/// source: OAuth flows, cloud managed identities, or custom security token services.
/// </summary>
/// <remarks>
/// Implementations must be thread-safe: providers are registered as singletons and invoked
/// concurrently. Implementations should cache tokens and perform single-flight refreshes.
/// </remarks>
public interface IAccessTokenProvider
{
    /// <summary>Gets a valid access token, refreshing it when required.</summary>
    /// <param name="forceRefresh">
    /// When <see langword="true"/>, bypasses any cached token and acquires a fresh one — used after
    /// a request was rejected with HTTP 401 despite a seemingly valid cached token.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the acquisition.</param>
    /// <returns>A valid access token.</returns>
    ValueTask<AccessToken> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken);
}
