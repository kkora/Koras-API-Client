using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Koras.ApiClient;

internal sealed class KorasApiClientBuilder : IKorasApiClientBuilder
{
    public KorasApiClientBuilder(
        string name,
        IServiceCollection services,
        IHttpClientBuilder httpClientBuilder,
        IHttpStandardResiliencePipelineBuilder resilienceBuilder)
    {
        Name = name;
        Services = services;
        HttpClientBuilder = httpClientBuilder;
        ResilienceBuilder = resilienceBuilder;
    }

    public string Name { get; }

    public IServiceCollection Services { get; }

    public IHttpClientBuilder HttpClientBuilder { get; }

    public IHttpStandardResiliencePipelineBuilder ResilienceBuilder { get; }
}
