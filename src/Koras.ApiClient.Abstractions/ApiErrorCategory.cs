namespace Koras.ApiClient;

/// <summary>
/// Classifies an <see cref="ApiError"/> into a coarse, actionable category so callers can branch
/// on failure kinds without inspecting raw status codes.
/// </summary>
/// <remarks>
/// This enum is open for additive evolution: treat unrecognized values as <see cref="Unknown"/>.
/// </remarks>
public enum ApiErrorCategory
{
    /// <summary>The failure could not be classified.</summary>
    Unknown = 0,

    /// <summary>A DNS, socket, TLS, or other connection-level failure occurred before a response was received.</summary>
    Network = 1,

    /// <summary>The request exceeded a client-side or per-request timeout, or the server returned 408.</summary>
    Timeout = 2,

    /// <summary>The response body could not be deserialized into the requested type.</summary>
    Serialization = 3,

    /// <summary>The server rejected the request as invalid (HTTP 400 or 422).</summary>
    Validation = 4,

    /// <summary>The request lacked valid credentials (HTTP 401), or a token could not be acquired.</summary>
    Authentication = 5,

    /// <summary>The credentials were valid but not permitted to perform the operation (HTTP 403).</summary>
    Authorization = 6,

    /// <summary>The target resource does not exist (HTTP 404 or 410).</summary>
    NotFound = 7,

    /// <summary>The request conflicts with the current state of the resource (HTTP 409 or 412).</summary>
    Conflict = 8,

    /// <summary>The caller is being throttled (HTTP 429); <see cref="ApiError.RetryAfter"/> may indicate when to retry.</summary>
    RateLimited = 9,

    /// <summary>Another client-side error (4xx) not covered by a more specific category.</summary>
    Client = 10,

    /// <summary>The server failed to process the request (5xx).</summary>
    Server = 11,
}
