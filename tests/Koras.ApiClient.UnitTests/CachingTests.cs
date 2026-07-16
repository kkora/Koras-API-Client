using System.Net;
using Koras.ApiClient.Testing;
using Koras.ApiClient.UnitTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Koras.ApiClient.UnitTests;

public class CachingTests
{
    private static (IApiClient Client, MockApiHandler Mock, ServiceProvider Provider) CreateClient(
        Action<IKorasApiClientBuilder>? configureBuilder = null)
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Get, "/data*").RespondWithJson(new Widget(1, "cached"));
        mock.When(HttpMethod.Post, "/data").RespondWithJson(new Widget(1, "posted"));
        var services = new ServiceCollection();
        var builder = services.AddKorasApiClient("cache-test", options => options.BaseAddress = new Uri("https://mock.local/"));
        configureBuilder?.Invoke(builder);
        builder.HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => mock);
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IApiClientFactory>().CreateClient("cache-test");
        return (client, mock, provider);
    }

    [Fact]
    public async Task WithCache_ServesSecondRequestFromCache()
    {
        var (client, mock, provider) = CreateClient();
        using (provider)
        {
            var first = await client.Get("data").WithCache(TimeSpan.FromMinutes(5)).SendAsync<Widget>();
            var second = await client.Get("data").WithCache(TimeSpan.FromMinutes(5)).SendAsync<Widget>();

            Assert.Equal(first.GetValueOrThrow(), second.GetValueOrThrow());
            Assert.Single(mock.Requests);
        }
    }

    [Fact]
    public async Task WithoutCacheOptIn_EveryRequestHitsTransport()
    {
        var (client, mock, provider) = CreateClient();
        using (provider)
        {
            await client.Get("data").SendAsync<Widget>();
            await client.Get("data").SendAsync<Widget>();

            Assert.Equal(2, mock.Requests.Count);
        }
    }

    [Fact]
    public async Task WithResponseCaching_EnablesClientWideCaching()
    {
        var (client, mock, provider) = CreateClient(builder => builder.WithResponseCaching());
        using (provider)
        {
            await client.Get("data").SendAsync<Widget>();
            await client.Get("data").SendAsync<Widget>();

            Assert.Single(mock.Requests);
        }
    }

    [Fact]
    public async Task DifferentUris_UseDifferentCacheEntries()
    {
        var (client, mock, provider) = CreateClient(builder => builder.WithResponseCaching());
        using (provider)
        {
            await client.Get("data").WithQuery("v", "1").SendAsync<Widget>();
            await client.Get("data").WithQuery("v", "2").SendAsync<Widget>();

            Assert.Equal(2, mock.Requests.Count);
        }
    }

    [Fact]
    public async Task NonGetRequests_AreNeverCached()
    {
        var (client, mock, provider) = CreateClient(builder => builder.WithResponseCaching());
        using (provider)
        {
            await client.Post("data").SendAsync<Widget>();
            await client.Post("data").SendAsync<Widget>();

            Assert.Equal(2, mock.Requests.Count);
        }
    }

    [Fact]
    public async Task ResponsesAboveSizeLimit_AreNotCached()
    {
        var (client, mock, provider) = CreateClient(builder =>
            builder.WithResponseCaching(caching => caching.MaxEntrySizeBytes = 1));
        using (provider)
        {
            await client.Get("data").SendAsync<Widget>();
            await client.Get("data").SendAsync<Widget>();

            Assert.Equal(2, mock.Requests.Count);
        }
    }

    [Fact]
    public async Task NonOkResponses_AreNotCached()
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Get, "/flaky").RespondWith(HttpStatusCode.NotFound).RespondWithJson(new Widget(1, "ok"));
        var services = new ServiceCollection();
        services.AddKorasApiClient("nc", options => options.BaseAddress = new Uri("https://mock.local/"))
            .WithResponseCaching()
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => mock);
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IApiClientFactory>().CreateClient("nc");

        var first = await client.Get("flaky").SendAsync<Widget>();
        var second = await client.Get("flaky").SendAsync<Widget>();

        Assert.False(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, mock.Requests.Count);
    }
}
