using System.Net;
using Koras.ApiClient.Testing;
using Koras.ApiClient.UnitTests.Support;
using Xunit;

namespace Koras.ApiClient.UnitTests;

public class MockApiHandlerTests
{
    [Fact]
    public async Task UnmatchedRequest_Returns404_ByDefault()
    {
        var handler = new MockApiHandler();
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("nothing-here").SendAsync();

        Assert.Equal(ApiErrorCategory.NotFound, response.Error!.Category);
    }

    [Fact]
    public async Task UnmatchedRequest_Throws_InStrictMode()
    {
        var handler = new MockApiHandler { ThrowOnUnmatchedRequest = true };
        var client = KorasApiClientTestFactory.Create(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.Get("nothing-here").SendAsync());
    }

    [Fact]
    public async Task ResponseSequence_RepeatsLastEntry()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/seq")
            .RespondWith(HttpStatusCode.ServiceUnavailable)
            .RespondWithJson(new Widget(1, "ok"));
        var client = KorasApiClientTestFactory.Create(handler);

        var first = await client.Get("seq").SendAsync<Widget>();
        var second = await client.Get("seq").SendAsync<Widget>();
        var third = await client.Get("seq").SendAsync<Widget>();

        Assert.Equal(ApiErrorCategory.Server, first.Error!.Category);
        Assert.True(second.IsSuccess);
        Assert.True(third.IsSuccess);
    }

    [Fact]
    public async Task WildcardPattern_MatchesPrefixes()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/users/*").RespondWithJson(new Widget(1, "user"));
        var client = KorasApiClientTestFactory.Create(handler);

        Assert.True((await client.Get("users/1").SendAsync<Widget>()).IsSuccess);
        Assert.True((await client.Get("users/42/details").SendAsync<Widget>()).IsSuccess);
        Assert.False((await client.Get("orders/1").SendAsync<Widget>()).IsSuccess);
    }

    [Fact]
    public async Task PathAndQueryPattern_MatchesExactQuery()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/items?page=2").RespondWithJson(new Widget(2, "page2"));
        var client = KorasApiClientTestFactory.Create(handler);

        Assert.True((await client.Get("items").WithQuery("page", 2).SendAsync<Widget>()).IsSuccess);
        Assert.False((await client.Get("items").WithQuery("page", 3).SendAsync<Widget>()).IsSuccess);
    }

    [Fact]
    public async Task RecordedRequests_IncludeBodiesAndHeaders()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Post, "/echo").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler);

        await client.Post("echo").WithHeader("X-Test", "yes").WithJsonBody(new Widget(9, "nine")).SendAsync();

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, recorded.Method);
        Assert.Contains("\"id\":9", recorded.Body);
        Assert.Equal("yes", recorded.Headers["X-Test"][0]);
    }
}
