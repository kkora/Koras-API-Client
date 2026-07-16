using Microsoft.Extensions.DependencyInjection;

namespace Koras.ApiClient;

/// <summary>
/// Configures one named Koras API client. Returned by <c>AddKorasApiClient</c> and extended by the
/// <c>With*</c>/<c>Configure*</c> methods (authentication, caching, resilience, typed clients).
/// </summary>
public interface IKorasApiClientBuilder
{
    /// <summary>The logical name of the client being configured.</summary>
    string Name { get; }

    /// <summary>The application service collection.</summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// The underlying <see cref="IHttpClientBuilder"/> — the escape hatch for adding custom
    /// delegating handlers, configuring the primary handler, or applying any standard
    /// <c>HttpClientFactory</c> extension.
    /// </summary>
    IHttpClientBuilder HttpClientBuilder { get; }
}
