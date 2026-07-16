using System.Net;
using System.Net.Http.Headers;
using Koras.ApiClient.Testing;
using Koras.ApiClient.UnitTests.Support;
using Xunit;

namespace Koras.ApiClient.UnitTests;

public class ErrorMappingTests
{
    [Theory]
    [InlineData(400, ApiErrorCategory.Validation)]
    [InlineData(422, ApiErrorCategory.Validation)]
    [InlineData(401, ApiErrorCategory.Authentication)]
    [InlineData(403, ApiErrorCategory.Authorization)]
    [InlineData(404, ApiErrorCategory.NotFound)]
    [InlineData(410, ApiErrorCategory.NotFound)]
    [InlineData(408, ApiErrorCategory.Timeout)]
    [InlineData(409, ApiErrorCategory.Conflict)]
    [InlineData(412, ApiErrorCategory.Conflict)]
    [InlineData(429, ApiErrorCategory.RateLimited)]
    [InlineData(418, ApiErrorCategory.Client)]
    [InlineData(500, ApiErrorCategory.Server)]
    [InlineData(503, ApiErrorCategory.Server)]
    public async Task StatusCodes_MapToDocumentedCategories(int statusCode, ApiErrorCategory expected)
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/e").RespondWith((HttpStatusCode)statusCode, "oops", "text/plain");
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("e").SendAsync();

        Assert.False(response.IsSuccess);
        Assert.Equal(expected, response.Error!.Category);
        Assert.Equal($"http_{statusCode}", response.Error.Code);
        Assert.Equal((HttpStatusCode)statusCode, response.Error.StatusCode);
        Assert.Equal("oops", response.Error.ResponseBody);
    }

    [Fact]
    public async Task ProblemDetails_IsParsedIntoProblemDocument()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/p").RespondWithProblem(
            new ProblemDocument
            {
                Title = "Validation failed",
                Detail = "Name is required.",
                Status = 400,
                Errors = new Dictionary<string, string[]> { ["name"] = ["Name is required."] },
            });
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("p").SendAsync();

        var error = response.Error!;
        Assert.Equal(ApiErrorCategory.Validation, error.Category);
        Assert.Equal("Validation failed", error.Message);
        Assert.NotNull(error.Problem);
        Assert.Equal("Validation failed", error.Problem!.Title);
        Assert.Equal(["Name is required."], error.Problem.Errors!["name"]);
    }

    [Fact]
    public async Task NonProblemJson_LeavesProblemNull()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/j").RespondWith(HttpStatusCode.BadRequest, """{"foo":"bar"}""");
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("j").SendAsync();

        Assert.Null(response.Error!.Problem);
        Assert.Contains("foo", response.Error.ResponseBody);
    }

    [Fact]
    public async Task RetryAfterHeader_IsSurfaced()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/r").RespondWith(HttpStatusCode.TooManyRequests, configure: response =>
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30)));
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("r").SendAsync();

        Assert.Equal(ApiErrorCategory.RateLimited, response.Error!.Category);
        Assert.Equal(TimeSpan.FromSeconds(30), response.Error.RetryAfter);
        Assert.True(response.Error.IsTransient);
    }

    [Fact]
    public async Task NetworkFailure_MapsToNetworkCategory_WithoutThrowing()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/n").Throws(new HttpRequestException("connection refused"));
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("n").SendAsync();

        Assert.False(response.IsSuccess);
        Assert.Null(response.StatusCode);
        Assert.Equal(ApiErrorCategory.Network, response.Error!.Category);
        Assert.IsType<HttpRequestException>(response.Error.Exception);
    }

    [Fact]
    public async Task TransportTimeout_MapsToTimeoutCategory()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/t").Throws(new TaskCanceledException("timed out"));
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("t").SendAsync();

        Assert.Equal(ApiErrorCategory.Timeout, response.Error!.Category);
        Assert.Equal("timeout", response.Error.Code);
    }

    [Fact]
    public async Task PerRequestTimeout_ProducesTimeoutError()
    {
        var client = KorasApiClientTestFactory.Create(new DelayHandler(TimeSpan.FromSeconds(10)));

        var response = await client.Get("slow").WithTimeout(TimeSpan.FromMilliseconds(50)).SendAsync();

        Assert.Equal(ApiErrorCategory.Timeout, response.Error!.Category);
    }

    [Fact]
    public async Task CallerCancellation_ThrowsOperationCanceledException()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/c").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.Get("c").SendAsync(cts.Token));
    }

    [Fact]
    public async Task MalformedJson_MapsToSerializationError()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/bad").RespondWith(HttpStatusCode.OK, "this is not json");
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("bad").SendAsync<Widget>();

        Assert.Equal(ApiErrorCategory.Serialization, response.Error!.Category);
        Assert.Equal("deserialization_failed", response.Error.Code);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NoContent_YieldsSuccessWithoutValue()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Delete, "/d").RespondWith(HttpStatusCode.NoContent);
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Delete("d").SendAsync<Widget>();

        Assert.True(response.IsSuccess);
        Assert.Null(response.Value);
    }

    [Fact]
    public async Task StringResponseType_ReturnsRawBody()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/raw").RespondWith(HttpStatusCode.OK, "plain text", "text/plain");
        var client = KorasApiClientTestFactory.Create(handler);

        var response = await client.Get("raw").SendAsync<string>();

        Assert.Equal("plain text", response.GetValueOrThrow());
    }

    [Fact]
    public async Task ErrorBodyCapture_IsBounded()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/big").RespondWith(HttpStatusCode.BadRequest, new string('x', 10_000), "text/plain");
        var client = KorasApiClientTestFactory.Create(handler, options => options.MaxErrorResponseBodyLength = 100);

        var response = await client.Get("big").SendAsync();

        Assert.Equal(100, response.Error!.ResponseBody!.Length);
    }

    [Fact]
    public async Task ErrorBodyCapture_CanBeDisabled()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/nb").RespondWith(HttpStatusCode.BadRequest, "secret-ish");
        var client = KorasApiClientTestFactory.Create(handler, options => options.CaptureErrorResponseBody = false);

        var response = await client.Get("nb").SendAsync();

        Assert.Null(response.Error!.ResponseBody);
    }
}
