using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace Koras.ApiClient.Http;

/// <summary>
/// Serves successful GET responses from an <see cref="IMemoryCache"/>. A request participates when
/// it carries a per-request duration (<c>WithCache</c>) or when client-wide caching is enabled.
/// Entries are keyed by client name and full request URI; only HTTP 200 responses within the
/// configured size limit are stored.
/// </summary>
internal sealed class ResponseCachingHandler : DelegatingHandler
{
    private readonly IMemoryCache _cache;
    private readonly ResponseCachingOptions _options;
    private readonly string _clientName;

    public ResponseCachingHandler(IMemoryCache cache, ResponseCachingOptions options, string clientName)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(clientName);
        _cache = cache;
        _options = options;
        _clientName = clientName;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Method != HttpMethod.Get)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        TimeSpan? duration = null;
        if (request.Options.TryGetValue(KorasHttpRequestOptions.CacheDuration, out var perRequestDuration) && perRequestDuration is { } explicitDuration)
        {
            duration = explicitDuration;
        }
        else if (_options.Enabled)
        {
            duration = _options.DefaultDuration;
        }

        if (duration is not { } timeToLive || timeToLive <= TimeSpan.Zero)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var key = $"Koras.ApiClient|{_clientName}|{request.RequestUri}";
        if (_cache.TryGetValue(key, out CachedResponse? cached) && cached is not null)
        {
            return cached.ToResponseMessage(request);
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return response;
        }

        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        var entry = CachedResponse.FromResponse(response, body);
        response.Dispose();
        if (body.LongLength <= _options.MaxEntrySizeBytes)
        {
            _cache.Set(key, entry, timeToLive);
        }

        return entry.ToResponseMessage(request);
    }

    private sealed class CachedResponse
    {
        private CachedResponse(HttpStatusCode statusCode, byte[] body, List<KeyValuePair<string, string[]>> headers, List<KeyValuePair<string, string[]>> contentHeaders)
        {
            StatusCode = statusCode;
            Body = body;
            Headers = headers;
            ContentHeaders = contentHeaders;
        }

        public HttpStatusCode StatusCode { get; }

        public byte[] Body { get; }

        public List<KeyValuePair<string, string[]>> Headers { get; }

        public List<KeyValuePair<string, string[]>> ContentHeaders { get; }

        public static CachedResponse FromResponse(HttpResponseMessage response, byte[] body)
        {
            var headers = new List<KeyValuePair<string, string[]>>();
            foreach (var header in response.Headers)
            {
                headers.Add(new KeyValuePair<string, string[]>(header.Key, [.. header.Value]));
            }

            var contentHeaders = new List<KeyValuePair<string, string[]>>();
            foreach (var header in response.Content.Headers)
            {
                contentHeaders.Add(new KeyValuePair<string, string[]>(header.Key, [.. header.Value]));
            }

            return new CachedResponse(response.StatusCode, body, headers, contentHeaders);
        }

        public HttpResponseMessage ToResponseMessage(HttpRequestMessage request)
        {
            var message = new HttpResponseMessage(StatusCode)
            {
                RequestMessage = request,
                Content = new ByteArrayContent(Body),
            };
            foreach (var header in Headers)
            {
                message.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var header in ContentHeaders)
            {
                message.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return message;
        }
    }
}
