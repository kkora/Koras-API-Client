using System.Net;

namespace Koras.ApiClient.Testing;

/// <summary>
/// An in-memory HTTP transport for tests: script responses per method and path with
/// <see cref="When(HttpMethod, string)"/>, then assert on the <see cref="Requests"/> it recorded.
/// Pair it with <see cref="KorasApiClientTestFactory"/> to exercise the real client pipeline.
/// </summary>
/// <remarks>
/// Path patterns match the URI's absolute path (case-insensitively); include a <c>?</c> to match
/// path-and-query, or end with <c>*</c> for a prefix match. Setups are evaluated in registration
/// order; unmatched requests return 404, or throw when <see cref="ThrowOnUnmatchedRequest"/> is set.
/// Thread-safe.
/// </remarks>
public sealed class MockApiHandler : HttpMessageHandler
{
    private readonly object _syncRoot = new();
    private readonly List<MockRequestSetup> _setups = [];
    private readonly List<RecordedRequest> _requests = [];

    /// <summary>When <see langword="true"/>, an unmatched request throws instead of returning 404 — strict mode for exhaustive tests.</summary>
    public bool ThrowOnUnmatchedRequest { get; set; }

    /// <summary>All requests received so far, in order.</summary>
    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_syncRoot)
            {
                return [.. _requests];
            }
        }
    }

    /// <summary>Registers a setup matching the given method and path pattern.</summary>
    /// <param name="method">The HTTP method to match.</param>
    /// <param name="pathPattern">
    /// The path to match (for example <c>/users/1</c>), a path-and-query (contains <c>?</c>), or a
    /// prefix pattern ending in <c>*</c>.
    /// </param>
    /// <returns>The setup to script responses on.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="pathPattern"/> is <see langword="null"/> or empty.</exception>
    public MockRequestSetup When(HttpMethod method, string pathPattern)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrEmpty(pathPattern);
        var setup = new MockRequestSetup(method, pathPattern);
        lock (_syncRoot)
        {
            _setups.Add(setup);
        }

        return setup;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string? body = null;
        if (request.Content is not null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        var recorded = new RecordedRequest(request.Method, request.RequestUri!, body, CollectHeaders(request));
        MockRequestSetup? match;
        lock (_syncRoot)
        {
            _requests.Add(recorded);
            match = _setups.Find(setup => setup.Matches(request));
        }

        if (match is null)
        {
            if (ThrowOnUnmatchedRequest)
            {
                throw new InvalidOperationException($"No MockApiHandler setup matches {request.Method} {request.RequestUri}.");
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                RequestMessage = request,
                Content = new StringContent($"No mock setup for {request.Method} {request.RequestUri}."),
            };
        }

        var response = match.CreateResponse();
        response.RequestMessage = request;
        return response;
    }

    private static Dictionary<string, string[]> CollectHeaders(HttpRequestMessage request)
    {
        var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            headers[header.Key] = [.. header.Value];
        }

        if (request.Content is not null)
        {
            foreach (var header in request.Content.Headers)
            {
                headers[header.Key] = [.. header.Value];
            }
        }

        return headers;
    }
}
