namespace Koras.ApiClient.Authentication;

/// <summary>
/// A delegating handler that attaches an API key to every request, as a header (default) or a
/// query-string parameter. Registered via <c>WithApiKey</c>; public for manual pipeline composition.
/// </summary>
public sealed class ApiKeyAuthenticationHandler : DelegatingHandler
{
    private readonly ApiKeyAuthenticationOptions _options;

    /// <summary>Initializes the handler with validated options.</summary>
    /// <param name="options">The API-key configuration; <see cref="ApiKeyAuthenticationOptions.ApiKey"/> must be set.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">No API key is configured.</exception>
    public ApiKeyAuthenticationHandler(ApiKeyAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrEmpty(options.ApiKey))
        {
            throw new ArgumentException("ApiKeyAuthenticationOptions.ApiKey must be configured.", nameof(options));
        }

        _options = options;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_options.Location == ApiKeyLocation.Header)
        {
            request.Headers.Remove(_options.HeaderName);
            request.Headers.TryAddWithoutValidation(_options.HeaderName, _options.ApiKey);
        }
        else if (request.RequestUri is { } uri)
        {
            var separator = string.IsNullOrEmpty(uri.Query) ? '?' : '&';
            request.RequestUri = new Uri(
                uri.AbsoluteUri + separator + Uri.EscapeDataString(_options.QueryParameterName) + "=" + Uri.EscapeDataString(_options.ApiKey!),
                UriKind.Absolute);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
