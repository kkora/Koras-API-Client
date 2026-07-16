using System.Net;
using Koras.ApiClient.Authentication;
using Koras.ApiClient.Testing;
using Koras.ApiClient.UnitTests.Support;
using Xunit;

namespace Koras.ApiClient.UnitTests;

public class AuthenticationTests
{
    [Fact]
    public async Task ApiKey_Header_IsAttached()
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Get, "/k").RespondWith(HttpStatusCode.OK);
        var handler = new ApiKeyAuthenticationHandler(new ApiKeyAuthenticationOptions { ApiKey = "secret-key" })
        {
            InnerHandler = mock,
        };
        var client = KorasApiClientTestFactory.Create(handler);

        await client.Get("k").SendAsync();

        Assert.Equal("secret-key", mock.Requests[0].Headers["X-Api-Key"][0]);
    }

    [Fact]
    public async Task ApiKey_CustomHeaderName_IsRespected()
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Get, "/k").RespondWith(HttpStatusCode.OK);
        var handler = new ApiKeyAuthenticationHandler(new ApiKeyAuthenticationOptions { ApiKey = "k1", HeaderName = "Api-Token" })
        {
            InnerHandler = mock,
        };
        var client = KorasApiClientTestFactory.Create(handler);

        await client.Get("k").SendAsync();

        Assert.Equal("k1", mock.Requests[0].Headers["Api-Token"][0]);
    }

    [Fact]
    public async Task ApiKey_QueryString_IsAppended()
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Get, "/k*").RespondWith(HttpStatusCode.OK);
        var handler = new ApiKeyAuthenticationHandler(new ApiKeyAuthenticationOptions
        {
            ApiKey = "qk",
            Location = ApiKeyLocation.QueryString,
        })
        {
            InnerHandler = mock,
        };
        var client = KorasApiClientTestFactory.Create(handler);

        await client.Get("k").WithQuery("a", "1").SendAsync();

        Assert.Contains("api_key=qk", mock.Requests[0].Uri.Query);
        Assert.Contains("a=1", mock.Requests[0].Uri.Query);
    }

    [Fact]
    public void ApiKey_MissingKey_FailsFast()
    {
        Assert.Throws<ArgumentException>(() => new ApiKeyAuthenticationHandler(new ApiKeyAuthenticationOptions()));
    }

    [Fact]
    public async Task BearerToken_IsAttached()
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Get, "/b").RespondWith(HttpStatusCode.OK);
        var handler = new BearerTokenHandler(new StaticTokenProvider("my-token")) { InnerHandler = mock };
        var client = KorasApiClientTestFactory.Create(handler);

        await client.Get("b").SendAsync();

        Assert.Equal("Bearer my-token", mock.Requests[0].Headers["Authorization"][0]);
    }

    [Fact]
    public async Task BearerToken_On401WithoutBody_RefreshesAndRetriesOnce()
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Get, "/r").RespondWith(HttpStatusCode.Unauthorized).RespondWithJson(new Widget(1, "ok"));
        var refreshFlags = new List<bool>();
        var provider = new DelegateTokenProvider((forceRefresh, _) =>
        {
            lock (refreshFlags)
            {
                refreshFlags.Add(forceRefresh);
            }

            return ValueTask.FromResult(new AccessToken(forceRefresh ? "fresh" : "stale", DateTimeOffset.MaxValue));
        });
        var handler = new BearerTokenHandler(provider) { InnerHandler = mock };
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("r").SendAsync<Widget>();

        Assert.True(response.IsSuccess);
        Assert.Equal(2, mock.Requests.Count);
        Assert.Equal([false, true], refreshFlags);
        Assert.Equal("Bearer fresh", mock.Requests[1].Headers["Authorization"][0]);
    }

    [Fact]
    public async Task BearerToken_On401WithBody_DoesNotRetry()
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Post, "/r").RespondWith(HttpStatusCode.Unauthorized);
        var handler = new BearerTokenHandler(new StaticTokenProvider("t")) { InnerHandler = mock };
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Post("r").WithJsonBody(new Widget(1, "x")).SendAsync();

        Assert.Equal(ApiErrorCategory.Authentication, response.Error!.Category);
        Assert.Single(mock.Requests);
    }

    [Fact]
    public void StaticTokenProvider_RejectsEmptyToken()
    {
        Assert.Throws<ArgumentException>(() => new StaticTokenProvider(string.Empty));
    }
}
