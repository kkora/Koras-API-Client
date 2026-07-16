namespace Koras.ApiClient;

/// <summary>
/// A transport-agnostic description of an API request. Instances are pure data: nothing is sent
/// until the request is passed to <see cref="IApiClient.SendAsync(ApiRequest, CancellationToken)"/>.
/// </summary>
/// <remarks>
/// Instances are not thread-safe and represent a single logical request. Most callers build
/// requests through the fluent <c>ApiRequestBuilder</c> in the <c>Koras.ApiClient</c> package
/// rather than constructing this type directly.
/// </remarks>
public sealed class ApiRequest
{
    /// <summary>Initializes a request with the specified HTTP method and path.</summary>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="path">
    /// The request path, absolute URL, or path-and-query. Relative paths are resolved against the
    /// client's configured base address.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
    public ApiRequest(HttpMethod method, string path)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(path);
        Method = method;
        Path = path;
    }

    /// <summary>The HTTP method of the request.</summary>
    public HttpMethod Method { get; }

    /// <summary>The request path, absolute URL, or path-and-query supplied at construction.</summary>
    public string Path { get; }

    /// <summary>
    /// Query-string parameters appended to <see cref="Path"/>. Names and values are escaped when
    /// the request is sent; parameters with a <see langword="null"/> value are omitted entirely
    /// (convenient for optional parameters such as pagination cursors). The same name may appear
    /// multiple times.
    /// </summary>
    public IList<KeyValuePair<string, string?>> QueryParameters { get; } = [];

    /// <summary>Request headers (case-insensitive names). Values here override client default headers.</summary>
    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The request body, or <see langword="null"/> for bodiless requests.</summary>
    public HttpContent? Content { get; set; }

    /// <summary>
    /// An optional per-request timeout that overrides the client-level timeout. When it elapses the
    /// operation completes with an <see cref="ApiError"/> of category <see cref="ApiErrorCategory.Timeout"/>.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// An explicit correlation identifier for this request. When <see langword="null"/> and
    /// correlation is enabled on the client, an identifier is generated automatically.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// When set on a GET request, marks the response as cacheable for the given duration
    /// (requires the caching pipeline of the <c>Koras.ApiClient</c> package).
    /// </summary>
    public TimeSpan? CacheDuration { get; set; }

    /// <summary>An extension bag for custom handlers and integrations. Keys should be namespaced.</summary>
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
