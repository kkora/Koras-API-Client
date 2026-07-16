using System.Net;

namespace Koras.ApiClient;

/// <summary>
/// A normalized description of a failed API operation. Every failure mode — transport errors,
/// timeouts, non-success HTTP statuses, and deserialization failures — is represented by this
/// single type so callers can handle outcomes uniformly.
/// </summary>
public sealed record ApiError
{
    /// <summary>The coarse classification of the failure.</summary>
    public required ApiErrorCategory Category { get; init; }

    /// <summary>
    /// A stable, machine-readable code such as <c>"http_404"</c>, <c>"network_error"</c>,
    /// <c>"timeout"</c>, or <c>"deserialization_failed"</c>.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>A human-readable description of the failure. Never contains credentials.</summary>
    public required string Message { get; init; }

    /// <summary>The HTTP status code of the response, when one was received.</summary>
    public HttpStatusCode? StatusCode { get; init; }

    /// <summary>The RFC 9457 problem details document carried by the response, when present and parseable.</summary>
    public ProblemDocument? Problem { get; init; }

    /// <summary>
    /// A bounded capture of the raw response body for diagnostics
    /// (see <c>KorasApiClientOptions.MaxErrorResponseBodyLength</c>). May be truncated or <see langword="null"/>.
    /// </summary>
    public string? ResponseBody { get; init; }

    /// <summary>The server-suggested retry delay, extracted from a <c>Retry-After</c> header when present.</summary>
    public TimeSpan? RetryAfter { get; init; }

    /// <summary>The underlying exception for transport, timeout, and deserialization failures.</summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Indicates whether retrying the operation later could plausibly succeed
    /// (network, timeout, rate-limit, and server errors).
    /// </summary>
    public bool IsTransient =>
        Category is ApiErrorCategory.Network
            or ApiErrorCategory.Timeout
            or ApiErrorCategory.RateLimited
            or ApiErrorCategory.Server;
}
