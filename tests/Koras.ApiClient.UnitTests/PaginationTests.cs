using System.Net;
using Koras.ApiClient.Testing;
using Koras.ApiClient.UnitTests.Support;
using Xunit;

namespace Koras.ApiClient.UnitTests;

public class PaginationTests
{
    [Fact]
    public async Task PaginateAsync_WalksCursorsUntilExhausted()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/items*")
            .RespondWithJson(new CursorPage([new Widget(1, "a"), new Widget(2, "b")], "c2"))
            .RespondWithJson(new CursorPage([new Widget(3, "c")], "c3"))
            .RespondWithJson(new CursorPage([new Widget(4, "d")], null));
        var client = KorasApiClientTestFactory.Create(handler);

        var items = new List<Widget>();
        await foreach (var item in client.PaginateAsync<CursorPage, Widget>(
            (c, cursor) => c.Get("items").WithQuery("cursor", cursor),
            page => new Page<Widget>(page.Items, page.Next)))
        {
            items.Add(item);
        }

        Assert.Equal(4, items.Count);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(string.Empty, handler.Requests[0].Uri.Query);
        Assert.Equal("?cursor=c2", handler.Requests[1].Uri.Query);
        Assert.Equal("?cursor=c3", handler.Requests[2].Uri.Query);
    }

    [Fact]
    public async Task PaginateAsync_EnforcesMaxPages()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/loop*").RespondWithJson(new CursorPage([new Widget(1, "x")], "again"));
        var client = KorasApiClientTestFactory.Create(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in client.PaginateAsync<CursorPage, Widget>(
                (c, cursor) => c.Get("loop").WithQuery("cursor", cursor),
                page => new Page<Widget>(page.Items, page.Next),
                maxPages: 3))
            {
            }
        });
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task PaginateAsync_FailedPage_ThrowsApiClientException()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/broken*")
            .RespondWithJson(new CursorPage([new Widget(1, "a")], "c2"))
            .RespondWith(HttpStatusCode.InternalServerError);
        var client = KorasApiClientTestFactory.Create(handler);

        var consumed = new List<Widget>();
        var exception = await Assert.ThrowsAsync<ApiClientException>(async () =>
        {
            await foreach (var item in client.PaginateAsync<CursorPage, Widget>(
                (c, cursor) => c.Get("broken").WithQuery("cursor", cursor),
                page => new Page<Widget>(page.Items, page.Next)))
            {
                consumed.Add(item);
            }
        });

        Assert.Single(consumed);
        Assert.Equal(ApiErrorCategory.Server, exception.Error.Category);
    }

    [Fact]
    public async Task PaginateAsync_HonorsCancellation()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/c*").RespondWithJson(new CursorPage([new Widget(1, "a")], "next"));
        var client = KorasApiClientTestFactory.Create(handler);
        using var cts = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in client.PaginateAsync<CursorPage, Widget>(
                (c, cursor) => c.Get("c").WithQuery("cursor", cursor),
                page => new Page<Widget>(page.Items, page.Next),
                cancellationToken: cts.Token))
            {
                await cts.CancelAsync();
            }
        });
    }

    [Fact]
    public async Task PaginateByPageNumber_StopsAtFirstEmptyPage()
    {
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/n*")
            .RespondWithJson(new NumberedPage([new Widget(1, "a"), new Widget(2, "b")]))
            .RespondWithJson(new NumberedPage([new Widget(3, "c")]))
            .RespondWithJson(new NumberedPage([]));
        var client = KorasApiClientTestFactory.Create(handler);

        var items = new List<Widget>();
        await foreach (var item in client.PaginateByPageNumberAsync<NumberedPage, Widget>("n", page => page.Items))
        {
            items.Add(item);
        }

        Assert.Equal(3, items.Count);
        Assert.Equal(["?page=1", "?page=2", "?page=3"], handler.Requests.Select(r => r.Uri.Query));
    }

    [Fact]
    public void PaginateAsync_ValidatesArgumentsEagerly()
    {
        var client = KorasApiClientTestFactory.Create(new MockApiHandler());

        Assert.Throws<ArgumentNullException>(() =>
            client.PaginateAsync<CursorPage, Widget>(null!, page => new Page<Widget>(page.Items, page.Next)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            client.PaginateAsync<CursorPage, Widget>(
                (c, cursor) => c.Get("x"),
                page => new Page<Widget>(page.Items, page.Next),
                maxPages: 0));
    }
}
