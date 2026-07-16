using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace Koras.ApiClient;

/// <summary>
/// Fluently composes and sends a single <see cref="ApiRequest"/>. Obtain instances via the verb
/// extension methods (<c>client.Get("path")</c>, <c>client.Post("path")</c>, …).
/// </summary>
/// <remarks>Not thread-safe: a builder represents one logical request — build, send, discard.</remarks>
public sealed class ApiRequestBuilder
{
    private readonly IApiClient _client;
    private readonly ApiRequest _request;

    internal ApiRequestBuilder(IApiClient client, HttpMethod method, string path)
    {
        _client = client;
        _request = new ApiRequest(method, path);
    }

    /// <summary>Appends a query-string parameter. Names and values are escaped automatically.</summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value; <see langword="null"/> omits the parameter entirely (convenient for optional values such as cursors).</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    public ApiRequestBuilder WithQuery(string name, string? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        _request.QueryParameters.Add(new KeyValuePair<string, string?>(name, value));
        return this;
    }

    /// <summary>Appends a query-string parameter, formatting the value with the invariant culture.</summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value; <see langword="null"/> omits the parameter entirely.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    public ApiRequestBuilder WithQuery(string name, object? value) =>
        WithQuery(name, value is null ? null : Convert.ToString(value, CultureInfo.InvariantCulture));

    /// <summary>Sets a request header, overriding any client default header with the same name.</summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public ApiRequestBuilder WithHeader(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);
        _request.Headers[name] = value;
        return this;
    }

    /// <summary>Sets an explicit correlation identifier for this request.</summary>
    /// <param name="correlationId">The correlation identifier to propagate.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentException"><paramref name="correlationId"/> is <see langword="null"/> or empty.</exception>
    public ApiRequestBuilder WithCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrEmpty(correlationId);
        _request.CorrelationId = correlationId;
        return this;
    }

    /// <summary>
    /// Sets a per-request timeout. When it elapses the operation completes with an
    /// <see cref="ApiError"/> of category <see cref="ApiErrorCategory.Timeout"/>.
    /// </summary>
    /// <param name="timeout">A positive timeout.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is not positive.</exception>
    public ApiRequestBuilder WithTimeout(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        _request.Timeout = timeout;
        return this;
    }

    /// <summary>Marks this GET request's successful response as cacheable for the given duration.</summary>
    /// <param name="duration">A positive cache duration.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is not positive.</exception>
    public ApiRequestBuilder WithCache(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        _request.CacheDuration = duration;
        return this;
    }

    /// <summary>Sets a JSON request body serialized with the client's options (or the supplied override).</summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="body">The value to serialize.</param>
    /// <param name="serializerOptions">Optional serializer options overriding the client defaults.</param>
    /// <returns>This builder.</returns>
    public ApiRequestBuilder WithJsonBody<T>(T body, JsonSerializerOptions? serializerOptions = null)
    {
        _request.Content = JsonContent.Create(body, options: serializerOptions ?? _client.JsonOptions);
        return this;
    }

    /// <summary>Sets an <c>application/x-www-form-urlencoded</c> request body.</summary>
    /// <param name="fields">The form fields.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is <see langword="null"/>.</exception>
    public ApiRequestBuilder WithFormUrlEncoded(IEnumerable<KeyValuePair<string, string>> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        _request.Content = new FormUrlEncodedContent(fields);
        return this;
    }

    /// <summary>Sets a <c>multipart/form-data</c> body composed of fields and file streams.</summary>
    /// <param name="configure">Configures the multipart content.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    public ApiRequestBuilder WithMultipart(Action<MultipartBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var multipart = new MultipartBuilder();
        configure(multipart);
        _request.Content = multipart.Build();
        return this;
    }

    /// <summary>Sets an arbitrary request body — the escape hatch for non-JSON payloads.</summary>
    /// <param name="content">The content to send.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is <see langword="null"/>.</exception>
    public ApiRequestBuilder WithContent(HttpContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        _request.Content = content;
        return this;
    }

    /// <summary>Returns the composed <see cref="ApiRequest"/> without sending it.</summary>
    /// <returns>The request described by this builder.</returns>
    public ApiRequest Build() => _request;

    /// <summary>Sends the request without deserializing the response body.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The normalized outcome.</returns>
    public Task<ApiResponse> SendAsync(CancellationToken cancellationToken = default) =>
        _client.SendAsync(_request, cancellationToken);

    /// <summary>Sends the request and deserializes a successful response body as <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The response body type (<see cref="string"/> receives the raw text).</typeparam>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The normalized outcome carrying the value on success.</returns>
    public Task<ApiResponse<T>> SendAsync<T>(CancellationToken cancellationToken = default) =>
        _client.SendAsync<T>(_request, cancellationToken);

    /// <summary>Sends the request and streams a successful response body into <paramref name="destination"/>.</summary>
    /// <param name="destination">The writable stream that receives the body.</param>
    /// <param name="progress">Optional progress callback reporting total bytes copied.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The normalized outcome.</returns>
    public Task<ApiResponse> DownloadAsync(Stream destination, IProgress<long>? progress = null, CancellationToken cancellationToken = default) =>
        _client.DownloadAsync(_request, destination, progress, cancellationToken);
}
