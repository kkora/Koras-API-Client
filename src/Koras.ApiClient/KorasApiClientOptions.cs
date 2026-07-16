using System.Text.Json;

namespace Koras.ApiClient;

/// <summary>
/// Configuration for a named Koras API client. Bindable from <c>IConfiguration</c> and validated at
/// startup when registered through <c>AddKorasApiClient</c>.
/// </summary>
public sealed class KorasApiClientOptions
{
    internal static readonly string DefaultUserAgent =
        "Koras.ApiClient/" + (typeof(KorasApiClientOptions).Assembly.GetName().Version?.ToString(3) ?? "0.0.0");

    /// <summary>The absolute base address that relative request paths are resolved against.</summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// The overall per-request time budget applied to the underlying <see cref="HttpClient"/>.
    /// Defaults to 30 seconds. The default resilience pipeline adds its own total (30 s) and
    /// per-attempt (10 s) timeouts — raise those via <c>ConfigureResilience</c> for long operations.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The <c>User-Agent</c> header value attached when no explicit header is set.
    /// Set to <see langword="null"/> to send no default user agent.
    /// </summary>
    public string? UserAgent { get; set; } = DefaultUserAgent;

    /// <summary>Headers attached to every request. Per-request headers with the same name take precedence.</summary>
    public IDictionary<string, string> DefaultHeaders { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// When <see langword="true"/> (default), every request carries a correlation identifier header,
    /// generated automatically when not supplied explicitly.
    /// </summary>
    public bool EnableCorrelationId { get; set; } = true;

    /// <summary>The header used for correlation identifiers. Defaults to <c>X-Correlation-ID</c>.</summary>
    public string CorrelationHeaderName { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// The serializer options for request and response JSON bodies.
    /// Defaults to <see cref="JsonSerializerDefaults.Web"/> (camelCase, case-insensitive).
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// When <see langword="true"/> (default), failure response bodies are captured onto
    /// <see cref="ApiError.ResponseBody"/> for diagnostics, truncated to <see cref="MaxErrorResponseBodyLength"/>.
    /// </summary>
    public bool CaptureErrorResponseBody { get; set; } = true;

    /// <summary>The maximum number of characters of a failure response body to capture. Defaults to 4096; 0 disables capture.</summary>
    public int MaxErrorResponseBodyLength { get; set; } = 4096;

    /// <summary>Response caching behavior for GET requests. Disabled unless enabled here or per request.</summary>
    public ResponseCachingOptions Caching { get; } = new();
}
