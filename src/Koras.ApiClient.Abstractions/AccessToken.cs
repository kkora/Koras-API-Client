namespace Koras.ApiClient;

/// <summary>An access token together with its expiry, as produced by an <see cref="IAccessTokenProvider"/>.</summary>
/// <param name="Value">The raw token value attached to requests as <c>Authorization: Bearer</c>. Never logged by the framework.</param>
/// <param name="ExpiresOn">
/// The instant after which the token must not be used. Providers typically subtract a refresh skew
/// so tokens are renewed shortly before their true expiry. Use <see cref="DateTimeOffset.MaxValue"/>
/// for non-expiring tokens.
/// </param>
public readonly record struct AccessToken(string Value, DateTimeOffset ExpiresOn);
