using Koras.ApiClient;
using Koras.ApiClient.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Authentication, caching, resilience, and typed-client configuration for a named Koras API client.</summary>
public static class KorasApiClientBuilderExtensions
{
    /// <summary>Attaches an API key to every request (header by default; see <see cref="ApiKeyLocation"/>).</summary>
    /// <param name="builder">The client builder.</param>
    /// <param name="configure">Configures the key, its location, and its parameter name. Validated at startup.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    public static IKorasApiClientBuilder WithApiKey(this IKorasApiClientBuilder builder, Action<ApiKeyAuthenticationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        builder.Services.AddOptions<ApiKeyAuthenticationOptions>(builder.Name)
            .Configure(configure)
            .Validate(options => !string.IsNullOrEmpty(options.ApiKey), "ApiKeyAuthenticationOptions.ApiKey must be configured.")
            .ValidateOnStart();
        var name = builder.Name;
        builder.HttpClientBuilder.AddHttpMessageHandler(sp =>
            new ApiKeyAuthenticationHandler(sp.GetRequiredService<IOptionsMonitor<ApiKeyAuthenticationOptions>>().Get(name)));
        return builder;
    }

    /// <summary>
    /// Attaches <c>Authorization: Bearer</c> tokens from a provider registered in the container.
    /// The provider is registered as a singleton if not already present.
    /// </summary>
    /// <typeparam name="TProvider">The token provider implementation.</typeparam>
    /// <param name="builder">The client builder.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IKorasApiClientBuilder WithBearerToken<TProvider>(this IKorasApiClientBuilder builder)
        where TProvider : class, IAccessTokenProvider
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.TryAddSingleton<TProvider>();
        builder.HttpClientBuilder.AddHttpMessageHandler(sp => new BearerTokenHandler(sp.GetRequiredService<TProvider>()));
        return builder;
    }

    /// <summary>Attaches <c>Authorization: Bearer</c> tokens from a provider produced by the factory.</summary>
    /// <param name="builder">The client builder.</param>
    /// <param name="providerFactory">Creates the token provider from the service provider.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    public static IKorasApiClientBuilder WithBearerToken(this IKorasApiClientBuilder builder, Func<IServiceProvider, IAccessTokenProvider> providerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(providerFactory);
        builder.HttpClientBuilder.AddHttpMessageHandler(sp => new BearerTokenHandler(providerFactory(sp)));
        return builder;
    }

    /// <summary>
    /// Attaches a fixed bearer token. Intended for development and testing; production flows should
    /// use <see cref="WithClientCredentials"/> or a custom <see cref="IAccessTokenProvider"/>.
    /// </summary>
    /// <param name="builder">The client builder.</param>
    /// <param name="token">The bearer token.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/> is <see langword="null"/> or empty.</exception>
    public static IKorasApiClientBuilder WithStaticBearerToken(this IKorasApiClientBuilder builder, string token)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var provider = new StaticTokenProvider(token);
        builder.HttpClientBuilder.AddHttpMessageHandler(() => new BearerTokenHandler(provider));
        return builder;
    }

    /// <summary>
    /// Authenticates with the OAuth 2.0 client-credentials grant: tokens are acquired from the
    /// configured endpoint over a dedicated resilient HTTP client, cached with expiry skew, and
    /// refreshed single-flight.
    /// </summary>
    /// <param name="builder">The client builder.</param>
    /// <param name="configure">Configures the token endpoint, client id/secret, and scopes. Validated at startup.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    public static IKorasApiClientBuilder WithClientCredentials(this IKorasApiClientBuilder builder, Action<ClientCredentialsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        var name = builder.Name;
        builder.Services.AddOptions<ClientCredentialsOptions>(name)
            .Configure(configure)
            .Validate(
                options => options.TokenEndpoint is not null && !string.IsNullOrEmpty(options.ClientId) && !string.IsNullOrEmpty(options.ClientSecret),
                "ClientCredentialsOptions requires TokenEndpoint, ClientId, and ClientSecret.")
            .ValidateOnStart();

        builder.Services.AddHttpClient(KorasApiClientDefaults.TokenHttpClientName(name)).AddStandardResilienceHandler();
        builder.Services.TryAddKeyedSingleton<IAccessTokenProvider>(
            KorasApiClientDefaults.TokenProviderKey(name),
            (sp, _) => new ClientCredentialsTokenProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(KorasApiClientDefaults.TokenHttpClientName(name)),
                sp.GetRequiredService<IOptionsMonitor<ClientCredentialsOptions>>().Get(name),
                sp.GetService<TimeProvider>()));
        builder.HttpClientBuilder.AddHttpMessageHandler(sp =>
            new BearerTokenHandler(sp.GetRequiredKeyedService<IAccessTokenProvider>(KorasApiClientDefaults.TokenProviderKey(name))));
        return builder;
    }

    /// <summary>
    /// Enables client-wide caching of successful GET responses (per-request opt-in via
    /// <c>WithCache</c> works without this call).
    /// </summary>
    /// <param name="builder">The client builder.</param>
    /// <param name="configure">Optional tuning of duration and entry-size limits.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IKorasApiClientBuilder WithResponseCaching(this IKorasApiClientBuilder builder, Action<ResponseCachingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Configure<KorasApiClientOptions>(builder.Name, options =>
        {
            options.Caching.Enabled = true;
            configure?.Invoke(options.Caching);
        });
        return builder;
    }

    /// <summary>
    /// Tunes the standard resilience pipeline (retry, circuit breaker, total and per-attempt
    /// timeouts) attached by <c>AddKorasApiClient</c>.
    /// </summary>
    /// <param name="builder">The client builder.</param>
    /// <param name="configure">Mutates the <see cref="HttpStandardResilienceOptions"/>.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The builder was not created by <c>AddKorasApiClient</c>.</exception>
    public static IKorasApiClientBuilder ConfigureResilience(this IKorasApiClientBuilder builder, Action<HttpStandardResilienceOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        if (builder is not KorasApiClientBuilder concrete)
        {
            throw new NotSupportedException("ConfigureResilience requires a builder created by AddKorasApiClient.");
        }

        concrete.ResilienceBuilder.Configure(configure);
        return builder;
    }

    /// <summary>
    /// Registers a transient typed client whose constructor receives this named
    /// <see cref="IApiClient"/> (remaining constructor parameters are resolved from the container).
    /// </summary>
    /// <typeparam name="TClient">The typed client class.</typeparam>
    /// <param name="builder">The client builder.</param>
    /// <param name="factory">Optional explicit factory receiving the API client and the service provider.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IKorasApiClientBuilder AddTypedClient<TClient>(this IKorasApiClientBuilder builder, Func<IApiClient, IServiceProvider, TClient>? factory = null)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        var name = builder.Name;
        builder.Services.AddTransient(sp =>
        {
            var apiClient = sp.GetRequiredService<IApiClientFactory>().CreateClient(name);
            return factory is not null
                ? factory(apiClient, sp)
                : ActivatorUtilities.CreateInstance<TClient>(sp, apiClient);
        });
        return builder;
    }
}
