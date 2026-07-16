using System.Net;
using Koras.ApiClient.Testing;
using Koras.ApiClient.UnitTests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Koras.ApiClient.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void DefaultOverload_MakesIApiClientInjectable()
    {
        var services = new ServiceCollection();
        services.AddKorasApiClient(options => options.BaseAddress = new Uri("https://api.local/"));
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IApiClient>();

        Assert.Equal(KorasApiClientServiceCollectionExtensions.DefaultClientName, client.Name);
    }

    [Fact]
    public void NamedClients_HaveIsolatedOptions()
    {
        var services = new ServiceCollection();
        services.AddKorasApiClient("a", options => options.BaseAddress = new Uri("https://a.local/"));
        services.AddKorasApiClient("b", options => options.BaseAddress = new Uri("https://b.local/"));
        using var provider = services.BuildServiceProvider();

        var monitor = provider.GetRequiredService<IOptionsMonitor<KorasApiClientOptions>>();
        Assert.Equal(new Uri("https://a.local/"), monitor.Get("a").BaseAddress);
        Assert.Equal(new Uri("https://b.local/"), monitor.Get("b").BaseAddress);

        var factory = provider.GetRequiredService<IApiClientFactory>();
        Assert.Equal("a", factory.CreateClient("a").Name);
        Assert.Equal("b", factory.CreateClient("b").Name);
    }

    [Fact]
    public async Task NamedClient_SendsThroughConfiguredPipeline()
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Get, "/w").RespondWithJson(new Widget(5, "five"));
        var services = new ServiceCollection();
        services.AddKorasApiClient("mocked", options => options.BaseAddress = new Uri("https://mock.local/"))
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => mock);
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IApiClientFactory>().CreateClient("mocked");
        var response = await client.Get("w").SendAsync<Widget>();

        Assert.Equal(new Widget(5, "five"), response.GetValueOrThrow());
    }

    [Fact]
    public void OptionsValidation_RejectsInvalidConfiguration()
    {
        var services = new ServiceCollection();
        services.AddKorasApiClient("bad", options => options.Timeout = TimeSpan.Zero);
        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<KorasApiClientOptions>>();

        var exception = Assert.Throws<OptionsValidationException>(() => monitor.Get("bad"));
        Assert.Contains("Timeout", exception.Message);
    }

    [Fact]
    public void ConfigurationBinding_PopulatesOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:BaseAddress"] = "https://cfg.local/",
                ["Api:Timeout"] = "00:00:05",
                ["Api:CorrelationHeaderName"] = "X-Request-ID",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddKorasApiClient("cfg", configuration.GetSection("Api"));
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptionsMonitor<KorasApiClientOptions>>().Get("cfg");

        Assert.Equal(new Uri("https://cfg.local/"), options.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(5), options.Timeout);
        Assert.Equal("X-Request-ID", options.CorrelationHeaderName);
    }

    [Fact]
    public async Task TypedClient_ReceivesNamedApiClient()
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Get, "/w/1").RespondWithJson(new Widget(1, "one"));
        var services = new ServiceCollection();
        services.AddKorasApiClient("widgets", options => options.BaseAddress = new Uri("https://mock.local/"))
            .AddTypedClient<WidgetApi>()
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => mock);
        using var provider = services.BuildServiceProvider();

        var api = provider.GetRequiredService<WidgetApi>();
        var widget = await api.GetAsync(1, CancellationToken.None);

        Assert.Equal("one", widget.Name);
        Assert.Equal("widgets", api.ClientName);
    }

    [Fact]
    public void ApiClientFactory_RejectsEmptyName()
    {
        var services = new ServiceCollection();
        services.AddKorasApiClient("x");
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IApiClientFactory>();

        Assert.Throws<ArgumentException>(() => factory.CreateClient(string.Empty));
    }

    [Fact]
    public void ConfigureResilience_AcceptsTuning()
    {
        var services = new ServiceCollection();
        var builder = services.AddKorasApiClient("tuned", options => options.BaseAddress = new Uri("https://t.local/"));

        // Must not throw; behavior is proven in the integration tests.
        builder.ConfigureResilience(resilience => resilience.Retry.MaxRetryAttempts = 2);
    }

    [Fact]
    public void WithApiKey_MissingKey_FailsValidation()
    {
        var services = new ServiceCollection();
        services.AddKorasApiClient("k", options => options.BaseAddress = new Uri("https://k.local/"))
            .WithApiKey(_ => { });
        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<Authentication.ApiKeyAuthenticationOptions>>();

        Assert.Throws<OptionsValidationException>(() => monitor.Get("k"));
    }

    public sealed class WidgetApi(IApiClient client)
    {
        public string ClientName => client.Name;

        public async Task<Widget> GetAsync(int id, CancellationToken cancellationToken)
        {
            var response = await client.Get($"w/{id}").SendAsync<Widget>(cancellationToken);
            return response.GetValueOrThrow();
        }
    }
}
