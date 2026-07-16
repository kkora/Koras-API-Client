namespace Koras.ApiClient;

/// <summary>
/// Creates named <see cref="IApiClient"/> instances, mirroring the semantics of
/// <c>IHttpClientFactory</c>. Implementations are thread-safe singletons.
/// </summary>
public interface IApiClientFactory
{
    /// <summary>Creates (or retrieves) the client with the specified logical name.</summary>
    /// <param name="name">The logical client name used at registration.</param>
    /// <returns>A configured client. Clients are lightweight and need not be cached by callers.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    IApiClient CreateClient(string name);
}
