namespace Koras.ApiClient;

/// <summary>
/// Controls the opt-in, memory-backed caching of successful GET responses.
/// </summary>
/// <remarks>
/// Cache entries are keyed by client name and full request URI. Do not enable client-wide caching
/// for endpoints whose responses vary per user or tenant unless that variance is part of the URI.
/// </remarks>
public sealed class ResponseCachingOptions
{
    /// <summary>
    /// When <see langword="true"/>, every successful GET response is cached for
    /// <see cref="DefaultDuration"/>. When <see langword="false"/> (default), only requests that
    /// opt in via <c>WithCache</c> are cached.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>The cache duration applied when <see cref="Enabled"/> is set and a request specifies no explicit duration. Defaults to 60 seconds.</summary>
    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Responses whose bodies exceed this size in bytes are never cached. Defaults to 1 MiB.</summary>
    public long MaxEntrySizeBytes { get; set; } = 1024 * 1024;
}
