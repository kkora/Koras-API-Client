using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Koras.ApiClient;

/// <summary>
/// The standard <see cref="IApiClient"/> implementation. Composes the request URI, applies default
/// headers and correlation identifiers, emits telemetry, and normalizes every failure into an
/// <see cref="ApiError"/>.
/// </summary>
/// <remarks>
/// Thread-safe. Applications usually obtain instances through dependency injection
/// (<c>AddKorasApiClient</c>) rather than this constructor; the public constructor exists for
/// advanced composition and for <c>Koras.ApiClient.Testing</c>.
/// </remarks>
public sealed class KorasApiClient : IApiClient
{
    private const int CopyBufferSize = 81920;

    private readonly HttpClient _httpClient;
    private readonly KorasApiClientOptions _options;
    private readonly ILogger _logger;

    /// <summary>Initializes a client over an existing <see cref="HttpClient"/>.</summary>
    /// <param name="name">The logical client name used in logs and telemetry tags.</param>
    /// <param name="httpClient">The underlying HTTP client (its <c>BaseAddress</c> and <c>Timeout</c> apply).</param>
    /// <param name="options">The client configuration.</param>
    /// <param name="logger">An optional logger; when omitted, logging is disabled.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="httpClient"/>, or <paramref name="options"/> is <see langword="null"/>.</exception>
    public KorasApiClient(string name, HttpClient httpClient, KorasApiClientOptions options, ILogger<KorasApiClient>? logger = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        Name = name;
        _httpClient = httpClient;
        _options = options;
        _logger = logger ?? NullLogger<KorasApiClient>.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public JsonSerializerOptions JsonOptions => _options.JsonSerializerOptions;

    /// <inheritdoc />
    public async Task<ApiResponse> SendAsync(ApiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var outcome = await ExecuteAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        if (outcome.Response is null)
        {
            return new ApiResponse { Error = outcome.Error, CorrelationId = outcome.CorrelationId };
        }

        using var response = outcome.Response;
        var headers = CollectHeaders(response);
        if (response.IsSuccessStatusCode)
        {
            return new ApiResponse { StatusCode = response.StatusCode, Headers = headers, CorrelationId = outcome.CorrelationId };
        }

        var error = await ApiErrorFactory.FromResponseAsync(response, _options, cancellationToken).ConfigureAwait(false);
        return new ApiResponse { StatusCode = response.StatusCode, Headers = headers, Error = error, CorrelationId = outcome.CorrelationId };
    }

    /// <inheritdoc />
    public async Task<ApiResponse<T>> SendAsync<T>(ApiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var outcome = await ExecuteAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        if (outcome.Response is null)
        {
            return new ApiResponse<T> { Error = outcome.Error, CorrelationId = outcome.CorrelationId };
        }

        using var response = outcome.Response;
        var headers = CollectHeaders(response);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ApiErrorFactory.FromResponseAsync(response, _options, cancellationToken).ConfigureAwait(false);
            return new ApiResponse<T> { StatusCode = response.StatusCode, Headers = headers, Error = error, CorrelationId = outcome.CorrelationId };
        }

        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength is 0)
        {
            return new ApiResponse<T> { StatusCode = response.StatusCode, Headers = headers, CorrelationId = outcome.CorrelationId };
        }

        try
        {
            T? value;
            if (typeof(T) == typeof(string))
            {
                var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                value = (T)(object)text;
            }
            else
            {
                value = await response.Content.ReadFromJsonAsync<T>(_options.JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
            }

            return new ApiResponse<T> { StatusCode = response.StatusCode, Headers = headers, Value = value, CorrelationId = outcome.CorrelationId };
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return new ApiResponse<T>
            {
                StatusCode = response.StatusCode,
                Headers = headers,
                Error = ApiErrorFactory.FromDeserialization(ex, response.StatusCode),
                CorrelationId = outcome.CorrelationId,
            };
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse> DownloadAsync(ApiRequest request, Stream destination, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(destination);
        var outcome = await ExecuteAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (outcome.Response is null)
        {
            return new ApiResponse { Error = outcome.Error, CorrelationId = outcome.CorrelationId };
        }

        using var response = outcome.Response;
        var headers = CollectHeaders(response);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ApiErrorFactory.FromResponseAsync(response, _options, cancellationToken).ConfigureAwait(false);
            return new ApiResponse { StatusCode = response.StatusCode, Headers = headers, Error = error, CorrelationId = outcome.CorrelationId };
        }

        try
        {
            var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (source.ConfigureAwait(false))
            {
                var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
                try
                {
                    long totalBytes = 0;
                    int bytesRead;
                    while ((bytesRead = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                        totalBytes += bytesRead;
                        progress?.Report(totalBytes);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException)
        {
            return new ApiResponse
            {
                StatusCode = response.StatusCode,
                Headers = headers,
                Error = ApiErrorFactory.FromNetworkError(ex),
                CorrelationId = outcome.CorrelationId,
            };
        }

        return new ApiResponse { StatusCode = response.StatusCode, Headers = headers, CorrelationId = outcome.CorrelationId };
    }

    private readonly record struct SendOutcome(HttpResponseMessage? Response, ApiError? Error, string? CorrelationId);

    private async Task<SendOutcome> ExecuteAsync(ApiRequest request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
    {
        using var httpRequest = CreateHttpRequest(request, out var correlationId);
        var method = request.Method.Method;
        var safePath = GetSafePath(request.Path);

        using var activity = ApiClientDiagnostics.ActivitySource.StartActivity($"{method} {Name}", ActivityKind.Client);
        if (activity is not null)
        {
            activity.SetTag("http.request.method", method);
            activity.SetTag("url.path", safePath);
            activity.SetTag("koras.client.name", Name);
            if (correlationId is not null)
            {
                activity.SetTag("koras.correlation_id", correlationId);
            }
        }

        Log.RequestStarting(_logger, method, safePath, Name, correlationId);
        var startTimestamp = Stopwatch.GetTimestamp();
        CancellationTokenSource? timeoutSource = null;
        try
        {
            var effectiveToken = cancellationToken;
            if (request.Timeout is { } timeout)
            {
                timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutSource.CancelAfter(timeout);
                effectiveToken = timeoutSource.Token;
            }

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(httpRequest, completionOption, effectiveToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                RecordFailure(activity, method, safePath, "canceled", "canceled", startTimestamp, correlationId);
                throw;
            }
            catch (OperationCanceledException ex)
            {
                var error = ApiErrorFactory.FromTimeout(ex);
                RecordFailure(activity, method, safePath, nameof(ApiErrorCategory.Timeout), error.Code, startTimestamp, correlationId);
                return new SendOutcome(null, error, correlationId);
            }
            catch (HttpRequestException ex)
            {
                var error = ApiErrorFactory.FromNetworkError(ex);
                RecordFailure(activity, method, safePath, nameof(ApiErrorCategory.Network), error.Code, startTimestamp, correlationId);
                return new SendOutcome(null, error, correlationId);
            }
            catch (ApiClientException ex)
            {
                // A pipeline component (for example a token provider) already produced a normalized error.
                RecordFailure(activity, method, safePath, ex.Error.Category.ToString(), ex.Error.Code, startTimestamp, correlationId);
                return new SendOutcome(null, ex.Error, correlationId);
            }

            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            var statusCode = (int)response.StatusCode;
            activity?.SetTag("http.response.status_code", statusCode);
            if (response.IsSuccessStatusCode)
            {
                Log.RequestCompleted(_logger, method, safePath, Name, statusCode, elapsed.TotalMilliseconds, correlationId);
                RecordMetrics(method, statusCode, null, elapsed);
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error);
                activity?.SetTag("error.type", $"http_{statusCode}");
                Log.RequestNonSuccess(_logger, method, safePath, Name, statusCode, elapsed.TotalMilliseconds, correlationId);
                RecordMetrics(method, statusCode, $"http_{statusCode}", elapsed);
            }

            return new SendOutcome(response, null, correlationId);
        }
        finally
        {
            timeoutSource?.Dispose();
        }
    }

    private void RecordFailure(Activity? activity, string method, string safePath, string errorCategory, string errorCode, long startTimestamp, string? correlationId)
    {
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        activity?.SetStatus(ActivityStatusCode.Error, errorCode);
        activity?.SetTag("error.type", errorCode);
        Log.RequestFailed(_logger, method, safePath, Name, errorCategory, errorCode, elapsed.TotalMilliseconds, correlationId);
        RecordMetrics(method, null, errorCode, elapsed);
    }

    private void RecordMetrics(string method, int? statusCode, string? errorType, TimeSpan elapsed)
    {
        var tags = new TagList
        {
            { "koras.client.name", Name },
            { "http.request.method", method },
        };
        if (statusCode is { } status)
        {
            tags.Add("http.response.status_code", status);
        }

        if (errorType is not null)
        {
            tags.Add("error.type", errorType);
        }

        ApiClientDiagnostics.Requests.Add(1, tags);
        ApiClientDiagnostics.RequestDuration.Record(elapsed.TotalMilliseconds, tags);
    }

    private HttpRequestMessage CreateHttpRequest(ApiRequest request, out string? correlationId)
    {
        var httpRequest = new HttpRequestMessage(request.Method, BuildRequestUri(request)) { Content = request.Content };

        foreach (var header in _options.DefaultHeaders)
        {
            if (!request.Headers.ContainsKey(header.Key))
            {
                AddHeader(httpRequest, header.Key, header.Value);
            }
        }

        foreach (var header in request.Headers)
        {
            AddHeader(httpRequest, header.Key, header.Value);
        }

        if (_options.UserAgent is { Length: > 0 } userAgent && !httpRequest.Headers.Contains("User-Agent"))
        {
            httpRequest.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        }

        correlationId = request.CorrelationId;
        if (_options.EnableCorrelationId)
        {
            correlationId ??= Guid.NewGuid().ToString("N");
        }

        if (correlationId is not null && !httpRequest.Headers.Contains(_options.CorrelationHeaderName))
        {
            httpRequest.Headers.TryAddWithoutValidation(_options.CorrelationHeaderName, correlationId);
        }

        if (request.CacheDuration is { } cacheDuration)
        {
            httpRequest.Options.Set(KorasHttpRequestOptions.CacheDuration, cacheDuration);
        }

        return httpRequest;
    }

    private static void AddHeader(HttpRequestMessage request, string name, string value)
    {
        if (!request.Headers.TryAddWithoutValidation(name, value) && request.Content is not null)
        {
            request.Content.Headers.TryAddWithoutValidation(name, value);
        }
    }

    private static Uri BuildRequestUri(ApiRequest request)
    {
        var path = request.Path;
        if (request.QueryParameters.Count > 0)
        {
            StringBuilder? builder = null;
            foreach (var parameter in request.QueryParameters)
            {
                if (parameter.Value is null)
                {
                    continue;
                }

                if (builder is null)
                {
                    builder = new StringBuilder(path);
                    builder.Append(path.Contains('?') ? '&' : '?');
                }
                else
                {
                    builder.Append('&');
                }

                builder.Append(Uri.EscapeDataString(parameter.Key))
                    .Append('=')
                    .Append(Uri.EscapeDataString(parameter.Value));
            }

            if (builder is not null)
            {
                path = builder.ToString();
            }
        }

        return new Uri(path, UriKind.RelativeOrAbsolute);
    }

    private static string GetSafePath(string path)
    {
        var queryIndex = path.IndexOf('?');
        return queryIndex < 0 ? path : path[..queryIndex];
    }

    private static Dictionary<string, IReadOnlyList<string>> CollectHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        return headers;
    }
}
