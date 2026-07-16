namespace Koras.ApiClient.Authentication;

/// <summary>
/// Adapts any asynchronous token source into an <see cref="IAccessTokenProvider"/> — the quickest
/// bridge for cloud credential SDKs (for example Azure managed identity via
/// <c>TokenCredential.GetTokenAsync</c>).
/// </summary>
public sealed class DelegateTokenProvider : IAccessTokenProvider
{
    private readonly Func<bool, CancellationToken, ValueTask<AccessToken>> _tokenFactory;

    /// <summary>Initializes the provider with the token factory.</summary>
    /// <param name="tokenFactory">
    /// Produces a token; receives the <c>forceRefresh</c> flag and the cancellation token.
    /// Must be thread-safe.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="tokenFactory"/> is <see langword="null"/>.</exception>
    public DelegateTokenProvider(Func<bool, CancellationToken, ValueTask<AccessToken>> tokenFactory)
    {
        ArgumentNullException.ThrowIfNull(tokenFactory);
        _tokenFactory = tokenFactory;
    }

    /// <inheritdoc />
    public ValueTask<AccessToken> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken) =>
        _tokenFactory(forceRefresh, cancellationToken);
}
