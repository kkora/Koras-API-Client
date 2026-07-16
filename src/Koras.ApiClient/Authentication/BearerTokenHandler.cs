using System.Net;
using System.Net.Http.Headers;

namespace Koras.ApiClient.Authentication;

/// <summary>
/// A delegating handler that attaches <c>Authorization: Bearer</c> tokens from an
/// <see cref="IAccessTokenProvider"/>. When a request without a body is rejected with HTTP 401, the
/// handler force-refreshes the token and retries exactly once. Requests with content are never
/// replayed, because their streams may already be consumed.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider _tokenProvider;

    /// <summary>Initializes the handler with the token source.</summary>
    /// <param name="tokenProvider">The provider supplying bearer tokens.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tokenProvider"/> is <see langword="null"/>.</exception>
    public BearerTokenHandler(IAccessTokenProvider tokenProvider)
    {
        ArgumentNullException.ThrowIfNull(tokenProvider);
        _tokenProvider = tokenProvider;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var token = await _tokenProvider.GetTokenAsync(forceRefresh: false, cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Unauthorized || request.Content is not null)
        {
            return response;
        }

        response.Dispose();
        var freshToken = await _tokenProvider.GetTokenAsync(forceRefresh: true, cancellationToken).ConfigureAwait(false);
        using var retryRequest = CloneBodilessRequest(request);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", freshToken.Value);
        return await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
    }

    private static HttpRequestMessage CloneBodilessRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy,
        };
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in (IDictionary<string, object?>)request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        return clone;
    }
}
