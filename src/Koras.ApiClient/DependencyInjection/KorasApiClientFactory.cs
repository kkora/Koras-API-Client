using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Koras.ApiClient;

/// <summary>Creates named <see cref="KorasApiClient"/> instances over <c>IHttpClientFactory</c>.</summary>
internal sealed class KorasApiClientFactory : IApiClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<KorasApiClientOptions> _optionsMonitor;
    private readonly ILoggerFactory _loggerFactory;

    public KorasApiClientFactory(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<KorasApiClientOptions> optionsMonitor,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
        _loggerFactory = loggerFactory;
    }

    public IApiClient CreateClient(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        var options = _optionsMonitor.Get(name);
        var httpClient = _httpClientFactory.CreateClient(KorasApiClientDefaults.HttpClientName(name));
        if (httpClient.BaseAddress is null && options.BaseAddress is not null)
        {
            httpClient.BaseAddress = options.BaseAddress;
        }

        return new KorasApiClient(name, httpClient, options, _loggerFactory.CreateLogger<KorasApiClient>());
    }
}
