namespace Koras.ApiClient.Authentication;

/// <summary>
/// Supplies a fixed, non-expiring bearer token. Intended for development, testing, and long-lived
/// personal access tokens — production OAuth flows should use <see cref="ClientCredentialsTokenProvider"/>
/// or a custom <see cref="IAccessTokenProvider"/>.
/// </summary>
public sealed class StaticTokenProvider : IAccessTokenProvider
{
    private readonly AccessToken _token;

    /// <summary>Initializes the provider with the fixed token value.</summary>
    /// <param name="token">The bearer token to attach to every request.</param>
    /// <exception cref="ArgumentException"><paramref name="token"/> is <see langword="null"/> or empty.</exception>
    public StaticTokenProvider(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        _token = new AccessToken(token, DateTimeOffset.MaxValue);
    }

    /// <inheritdoc />
    public ValueTask<AccessToken> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken) =>
        ValueTask.FromResult(_token);
}
