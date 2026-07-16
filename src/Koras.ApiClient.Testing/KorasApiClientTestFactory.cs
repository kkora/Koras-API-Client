namespace Koras.ApiClient.Testing;

/// <summary>
/// Builds real <see cref="KorasApiClient"/> instances over an arbitrary transport (typically a
/// <see cref="MockApiHandler"/>) without dependency injection — tests exercise the framework's true
/// serialization, correlation, and error-normalization pipeline.
/// </summary>
public static class KorasApiClientTestFactory
{
    /// <summary>Creates a client over the given transport handler.</summary>
    /// <param name="handler">The transport (usually a <see cref="MockApiHandler"/>). Not disposed by the returned client.</param>
    /// <param name="configure">Optional options customization. The default base address is <c>https://mock.local/</c>.</param>
    /// <param name="name">The logical client name used in logs and telemetry. Defaults to <c>Test</c>.</param>
    /// <returns>A fully functional API client backed by <paramref name="handler"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    public static IApiClient Create(HttpMessageHandler handler, Action<KorasApiClientOptions>? configure = null, string name = "Test")
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var options = new KorasApiClientOptions { BaseAddress = new Uri("https://mock.local/") };
        configure?.Invoke(options);

        var httpClient = new HttpClient(handler, disposeHandler: false) { Timeout = options.Timeout };
        if (options.BaseAddress is not null)
        {
            httpClient.BaseAddress = options.BaseAddress;
        }

        return new KorasApiClient(name, httpClient, options);
    }
}
