using Koras.ApiClient;
using Koras.ApiClient.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers Koras API clients on the service collection.</summary>
public static class KorasApiClientServiceCollectionExtensions
{
    /// <summary>The client name used by the single-client <see cref="AddKorasApiClient(IServiceCollection, Action{KorasApiClientOptions})"/> overload.</summary>
    public const string DefaultClientName = KorasApiClientDefaults.DefaultClientName;

    /// <summary>
    /// Registers the default client and makes <see cref="IApiClient"/> directly injectable.
    /// Use the named overload when the application talks to multiple APIs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the client options (set <see cref="KorasApiClientOptions.BaseAddress"/> here).</param>
    /// <returns>A builder for further configuration (authentication, caching, resilience).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IKorasApiClientBuilder AddKorasApiClient(this IServiceCollection services, Action<KorasApiClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = services.AddKorasApiClient(DefaultClientName, configure);
        services.TryAddSingleton<IApiClient>(sp => sp.GetRequiredService<IApiClientFactory>().CreateClient(DefaultClientName));
        return builder;
    }

    /// <summary>
    /// Registers a named client resolvable through <see cref="IApiClientFactory.CreateClient(string)"/>.
    /// The standard resilience pipeline (retry with jitter, circuit breaker, timeouts) and the
    /// response-caching pipeline are attached automatically; caching stays inert until enabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The logical client name.</param>
    /// <param name="configure">Optional configuration of the client options.</param>
    /// <returns>A builder for further configuration (authentication, caching, resilience).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    public static IKorasApiClientBuilder AddKorasApiClient(this IServiceCollection services, string name, Action<KorasApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var optionsBuilder = services.AddOptions<KorasApiClientOptions>(name);
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        optionsBuilder.ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<KorasApiClientOptions>, KorasApiClientOptionsValidator>());
        services.TryAddSingleton<IApiClientFactory, KorasApiClientFactory>();
        services.AddMemoryCache();

        var httpClientBuilder = services
            .AddHttpClient(KorasApiClientDefaults.HttpClientName(name))
            .ConfigureHttpClient((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptionsMonitor<KorasApiClientOptions>>().Get(name);
                if (options.BaseAddress is not null)
                {
                    http.BaseAddress = options.BaseAddress;
                }

                http.Timeout = options.Timeout;
            })
            .AddHttpMessageHandler(sp => new ResponseCachingHandler(
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<IOptionsMonitor<KorasApiClientOptions>>().Get(name).Caching,
                name));

        var resilienceBuilder = httpClientBuilder.AddStandardResilienceHandler();
        return new KorasApiClientBuilder(name, services, httpClientBuilder, resilienceBuilder);
    }

    /// <summary>
    /// Registers a named client with options bound from a configuration section
    /// (for example <c>configuration.GetSection("Clients:Vendor")</c>), then applies
    /// <paramref name="configure"/> on top.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The logical client name.</param>
    /// <param name="configuration">The configuration section to bind <see cref="KorasApiClientOptions"/> from.</param>
    /// <param name="configure">Optional code configuration applied after binding.</param>
    /// <returns>A builder for further configuration (authentication, caching, resilience).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    public static IKorasApiClientBuilder AddKorasApiClient(this IServiceCollection services, string name, IConfiguration configuration, Action<KorasApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddOptions<KorasApiClientOptions>(name).Bind(configuration);
        return services.AddKorasApiClient(name, configure);
    }
}
