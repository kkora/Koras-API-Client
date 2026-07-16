using System.Net;
using Koras.ApiClient.Testing;
using Koras.ApiClient.UnitTests.Support;
using Xunit;

namespace Koras.ApiClient.UnitTests;

public class RequestBuilderTests
{
    [Fact]
    public async Task WithQuery_EscapesNamesAndValues()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/search*").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler);

        await client.Get("search").WithQuery("q", "a b&c=d").SendAsync();

        Assert.Equal("?q=a%20b%26c%3Dd", handler.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task WithQuery_SupportsMultipleValuesForSameName()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/items*").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler);

        await client.Get("items").WithQuery("tag", "a").WithQuery("tag", "b").SendAsync();

        Assert.Equal("?tag=a&tag=b", handler.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task WithQuery_NullValue_OmitsParameter()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/items*").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler);

        await client.Get("items").WithQuery("cursor", (string?)null).WithQuery("size", 10).SendAsync();

        Assert.Equal("?size=10", handler.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task WithQuery_AppendsToExistingQueryString()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/a*").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler);

        await client.Get("a?x=1").WithQuery("y", "2").SendAsync();

        Assert.Equal("/a?x=1&y=2", handler.Requests[0].Uri.PathAndQuery);
    }

    [Fact]
    public async Task WithHeader_OverridesDefaultHeader()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/h").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler, options => options.DefaultHeaders["X-Tenant"] = "default");

        await client.Get("h").WithHeader("X-Tenant", "override").SendAsync();

        Assert.Equal("override", Assert.Single(handler.Requests[0].Headers["X-Tenant"]));
    }

    [Fact]
    public async Task DefaultUserAgent_IsAttached()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/ua").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler);

        await client.Get("ua").SendAsync();

        Assert.StartsWith("Koras.ApiClient/", handler.Requests[0].Headers["User-Agent"][0]);
    }

    [Fact]
    public async Task WithJsonBody_SerializesWithWebDefaults()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Post, "/items").RespondWithJson(new Widget(1, "a"), HttpStatusCode.Created);
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Post("items").WithJsonBody(new Widget(7, "gadget")).SendAsync<Widget>();

        Assert.True(response.IsSuccess);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var recorded = handler.Requests[0];
        Assert.Contains("\"id\":7", recorded.Body);
        Assert.Contains("\"name\":\"gadget\"", recorded.Body);
        Assert.StartsWith("application/json", recorded.Headers["Content-Type"][0]);
    }

    [Fact]
    public async Task WithFormUrlEncoded_SendsFormContent()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Post, "/form").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler);

        await client.Post("form").WithFormUrlEncoded([new("a", "1"), new("b", "two words")]).SendAsync();

        var recorded = handler.Requests[0];
        Assert.StartsWith("application/x-www-form-urlencoded", recorded.Headers["Content-Type"][0]);
        Assert.Contains("a=1", recorded.Body);
    }

    [Fact]
    public async Task CorrelationId_IsGeneratedAndEchoedOnResponse()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/c").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("c").SendAsync();

        Assert.False(string.IsNullOrEmpty(response.CorrelationId));
        Assert.Equal(response.CorrelationId, handler.Requests[0].Headers["X-Correlation-ID"][0]);
    }

    [Fact]
    public async Task CorrelationId_ExplicitValue_IsUsed()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/c").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("c").WithCorrelationId("my-correlation").SendAsync();

        Assert.Equal("my-correlation", response.CorrelationId);
        Assert.Equal("my-correlation", handler.Requests[0].Headers["X-Correlation-ID"][0]);
    }

    [Fact]
    public async Task CorrelationId_Disabled_SendsNoHeader()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/c").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler, options => options.EnableCorrelationId = false);

        var response = await client.Get("c").SendAsync();

        Assert.Null(response.CorrelationId);
        Assert.False(handler.Requests[0].Headers.ContainsKey("X-Correlation-ID"));
    }

    [Fact]
    public void WithTimeout_RejectsNonPositiveValues()
    {
        var client = KorasApiClientTestFactory.Create(new MockApiHandler());
        Assert.Throws<ArgumentOutOfRangeException>(() => client.Get("x").WithTimeout(TimeSpan.Zero));
    }

    [Fact]
    public void WithQuery_RejectsEmptyName()
    {
        var client = KorasApiClientTestFactory.Create(new MockApiHandler());
        Assert.Throws<ArgumentException>(() => client.Get("x").WithQuery(string.Empty, "v"));
    }

    [Fact]
    public async Task Verbs_MapToHttpMethods()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/v").RespondWith(HttpStatusCode.OK);
        handler.When(HttpMethod.Post, "/v").RespondWith(HttpStatusCode.OK);
        handler.When(HttpMethod.Put, "/v").RespondWith(HttpStatusCode.OK);
        handler.When(HttpMethod.Patch, "/v").RespondWith(HttpStatusCode.OK);
        handler.When(HttpMethod.Delete, "/v").RespondWith(HttpStatusCode.OK);
        handler.When(HttpMethod.Head, "/v").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler);

        await client.Get("v").SendAsync();
        await client.Post("v").SendAsync();
        await client.Put("v").SendAsync();
        await client.Patch("v").SendAsync();
        await client.Delete("v").SendAsync();
        await client.Head("v").SendAsync();

        Assert.Equal(
            [HttpMethod.Get, HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete, HttpMethod.Head],
            handler.Requests.Select(r => r.Method));
    }

    [Fact]
    public async Task GetFromJsonAsync_Convenience_Works()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/w/1").RespondWithJson(new Widget(1, "one"));
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.GetFromJsonAsync<Widget>("w/1");

        Assert.Equal(new Widget(1, "one"), response.GetValueOrThrow());
    }
}
