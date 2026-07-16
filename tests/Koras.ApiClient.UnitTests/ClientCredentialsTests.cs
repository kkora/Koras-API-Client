using System.Net;
using Koras.ApiClient.Authentication;
using Koras.ApiClient.Testing;
using Koras.ApiClient.UnitTests.Support;
using Xunit;

namespace Koras.ApiClient.UnitTests;

public class ClientCredentialsTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static ClientCredentialsOptions ValidOptions()
    {
        var options = new ClientCredentialsOptions
        {
            TokenEndpoint = new Uri("https://sts.local/token"),
            ClientId = "id",
            ClientSecret = "secret",
        };
        options.Scopes.Add("read:widgets");
        return options;
    }

    private static MockApiHandler TokenEndpoint(long expiresIn = 3600)
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Post, "/token").RespondWithJson(new { access_token = "tok-1", expires_in = expiresIn, token_type = "Bearer" });
        return mock;
    }

    [Fact]
    public async Task RequestsToken_WithClientCredentialsGrant()
    {
        var mock = TokenEndpoint();
        using var provider = new ClientCredentialsTokenProvider(new HttpClient(mock), ValidOptions(), new FakeTimeProvider(Start));

        var token = await provider.GetTokenAsync(forceRefresh: false, CancellationToken.None);

        Assert.Equal("tok-1", token.Value);
        var body = mock.Requests[0].Body!;
        Assert.Contains("grant_type=client_credentials", body);
        Assert.Contains("client_id=id", body);
        Assert.Contains("scope=read", body);
    }

    [Fact]
    public async Task CachesToken_UntilExpirySkew()
    {
        var mock = TokenEndpoint(expiresIn: 100);
        var time = new FakeTimeProvider(Start);
        using var provider = new ClientCredentialsTokenProvider(new HttpClient(mock), ValidOptions(), time);

        await provider.GetTokenAsync(false, CancellationToken.None);
        await provider.GetTokenAsync(false, CancellationToken.None);
        Assert.Single(mock.Requests);

        // 100s lifetime - 60s skew = fresh for 40s; advance past it.
        time.Advance(TimeSpan.FromSeconds(50));
        await provider.GetTokenAsync(false, CancellationToken.None);
        Assert.Equal(2, mock.Requests.Count);
    }

    [Fact]
    public async Task ForceRefresh_BypassesCache()
    {
        var mock = TokenEndpoint();
        using var provider = new ClientCredentialsTokenProvider(new HttpClient(mock), ValidOptions(), new FakeTimeProvider(Start));

        await provider.GetTokenAsync(false, CancellationToken.None);
        await provider.GetTokenAsync(true, CancellationToken.None);

        Assert.Equal(2, mock.Requests.Count);
    }

    [Fact]
    public async Task ConcurrentColdStarts_ShareOneTokenRequest()
    {
        var mock = TokenEndpoint();
        using var provider = new ClientCredentialsTokenProvider(new HttpClient(mock), ValidOptions(), new FakeTimeProvider(Start));

        var tokens = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => provider.GetTokenAsync(false, CancellationToken.None).AsTask()));

        Assert.All(tokens, token => Assert.Equal("tok-1", token.Value));
        Assert.Single(mock.Requests);
    }

    [Fact]
    public async Task TokenEndpointFailure_ThrowsNormalizedAuthenticationError()
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Post, "/token").RespondWith(HttpStatusCode.InternalServerError);
        using var provider = new ClientCredentialsTokenProvider(new HttpClient(mock), ValidOptions(), new FakeTimeProvider(Start));

        var exception = await Assert.ThrowsAsync<ApiClientException>(
            () => provider.GetTokenAsync(false, CancellationToken.None).AsTask());

        Assert.Equal(ApiErrorCategory.Authentication, exception.Error.Category);
        Assert.Equal("token_request_failed", exception.Error.Code);
    }

    [Fact]
    public async Task TokenFailure_SurfacesAsErrorResponse_ThroughClientPipeline()
    {
        var api = new MockApiHandler();
        api.When(HttpMethod.Get, "/data").RespondWithJson(new Widget(1, "x"));
        var sts = new MockApiHandler();
        sts.When(HttpMethod.Post, "/token").RespondWith(HttpStatusCode.Forbidden);
        using var provider = new ClientCredentialsTokenProvider(new HttpClient(sts), ValidOptions(), new FakeTimeProvider(Start));
        var handler = new BearerTokenHandler(provider) { InnerHandler = api };
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("data").SendAsync<Widget>();

        Assert.False(response.IsSuccess);
        Assert.Equal(ApiErrorCategory.Authentication, response.Error!.Category);
        Assert.Empty(api.Requests);
    }

    [Fact]
    public async Task InvalidTokenResponse_Throws()
    {
        var mock = new MockApiHandler();
        mock.When(HttpMethod.Post, "/token").RespondWith(HttpStatusCode.OK, """{"unexpected":"shape"}""");
        using var provider = new ClientCredentialsTokenProvider(new HttpClient(mock), ValidOptions(), new FakeTimeProvider(Start));

        var exception = await Assert.ThrowsAsync<ApiClientException>(
            () => provider.GetTokenAsync(false, CancellationToken.None).AsTask());

        Assert.Equal("token_response_invalid", exception.Error.Code);
    }

    [Fact]
    public void MissingRequiredOptions_FailFast()
    {
        Assert.Throws<ArgumentException>(() =>
            new ClientCredentialsTokenProvider(new HttpClient(new MockApiHandler()), new ClientCredentialsOptions()));
    }
}
