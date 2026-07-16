using System.Runtime.CompilerServices;

namespace Koras.ApiClient;

/// <summary>
/// Consumes paginated APIs as <see cref="IAsyncEnumerable{T}"/> streams with bounded page counts
/// and full cancellation support. Any failed page surfaces as an <see cref="ApiClientException"/>
/// so enumeration never silently truncates.
/// </summary>
public static class ApiClientPaginationExtensions
{
    /// <summary>The default upper bound on pages fetched per enumeration, guarding against misbehaving APIs.</summary>
    public const int DefaultMaxPages = 1000;

    /// <summary>Enumerates all items of a cursor-paginated API.</summary>
    /// <typeparam name="TPage">The type each page response deserializes into.</typeparam>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="client">The API client.</param>
    /// <param name="requestFactory">
    /// Builds the request for a page given the cursor (<see langword="null"/> for the first page),
    /// for example <c>(c, cursor) =&gt; c.Get("orders").WithQuery("cursor", cursor)</c>.
    /// </param>
    /// <param name="pageSelector">Extracts the items and next cursor from a deserialized page.</param>
    /// <param name="maxPages">The maximum number of pages to fetch before failing. Defaults to <see cref="DefaultMaxPages"/>.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous stream of all items across pages.</returns>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPages"/> is less than 1.</exception>
    /// <exception cref="ApiClientException">A page request failed (thrown during enumeration).</exception>
    /// <exception cref="InvalidOperationException"><paramref name="maxPages"/> pages were fetched without reaching the end (thrown during enumeration).</exception>
    public static IAsyncEnumerable<TItem> PaginateAsync<TPage, TItem>(
        this IApiClient client,
        Func<IApiClient, string?, ApiRequestBuilder> requestFactory,
        Func<TPage, Page<TItem>> pageSelector,
        int maxPages = DefaultMaxPages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestFactory);
        ArgumentNullException.ThrowIfNull(pageSelector);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPages, 1);
        return IterateAsync(client, requestFactory, pageSelector, maxPages, cancellationToken);
    }

    /// <summary>Enumerates all items of a page-number-paginated API, stopping at the first empty page.</summary>
    /// <typeparam name="TPage">The type each page response deserializes into.</typeparam>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="client">The API client.</param>
    /// <param name="path">The request path queried for every page.</param>
    /// <param name="itemsSelector">Extracts the items from a deserialized page.</param>
    /// <param name="pageParameterName">The query parameter carrying the page number. Defaults to <c>page</c>.</param>
    /// <param name="firstPageNumber">The number of the first page. Defaults to 1.</param>
    /// <param name="configureRequest">Optional per-page request customization (extra query parameters, headers).</param>
    /// <param name="maxPages">The maximum number of pages to fetch before failing. Defaults to <see cref="DefaultMaxPages"/>.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous stream of all items across pages.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/>, <paramref name="path"/>, or <paramref name="itemsSelector"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="pageParameterName"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPages"/> is less than 1.</exception>
    /// <exception cref="ApiClientException">A page request failed (thrown during enumeration).</exception>
    /// <exception cref="InvalidOperationException"><paramref name="maxPages"/> pages were fetched without reaching an empty page (thrown during enumeration).</exception>
    public static IAsyncEnumerable<TItem> PaginateByPageNumberAsync<TPage, TItem>(
        this IApiClient client,
        string path,
        Func<TPage, IReadOnlyList<TItem>> itemsSelector,
        string pageParameterName = "page",
        int firstPageNumber = 1,
        Action<ApiRequestBuilder>? configureRequest = null,
        int maxPages = DefaultMaxPages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(itemsSelector);
        ArgumentException.ThrowIfNullOrEmpty(pageParameterName);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPages, 1);
        return IterateByPageNumberAsync(client, path, itemsSelector, pageParameterName, firstPageNumber, configureRequest, maxPages, cancellationToken);
    }

    private static async IAsyncEnumerable<TItem> IterateAsync<TPage, TItem>(
        IApiClient client,
        Func<IApiClient, string?, ApiRequestBuilder> requestFactory,
        Func<TPage, Page<TItem>> pageSelector,
        int maxPages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? cursor = null;
        for (var fetchedPages = 0; fetchedPages < maxPages; fetchedPages++)
        {
            var response = await requestFactory(client, cursor).SendAsync<TPage>(cancellationToken).ConfigureAwait(false);
            var page = pageSelector(response.GetValueOrThrow());
            foreach (var item in page.Items)
            {
                yield return item;
            }

            if (string.IsNullOrEmpty(page.NextCursor))
            {
                yield break;
            }

            cursor = page.NextCursor;
        }

        throw new InvalidOperationException(
            $"Pagination did not complete within {maxPages} pages. Raise maxPages if the collection is genuinely that large, or verify the API's cursor semantics.");
    }

    private static async IAsyncEnumerable<TItem> IterateByPageNumberAsync<TPage, TItem>(
        IApiClient client,
        string path,
        Func<TPage, IReadOnlyList<TItem>> itemsSelector,
        string pageParameterName,
        int firstPageNumber,
        Action<ApiRequestBuilder>? configureRequest,
        int maxPages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var fetchedPages = 0; fetchedPages < maxPages; fetchedPages++)
        {
            var pageNumber = firstPageNumber + fetchedPages;
            var builder = client.Get(path).WithQuery(pageParameterName, (object)pageNumber);
            configureRequest?.Invoke(builder);
            var response = await builder.SendAsync<TPage>(cancellationToken).ConfigureAwait(false);
            var items = itemsSelector(response.GetValueOrThrow());
            if (items.Count == 0)
            {
                yield break;
            }

            foreach (var item in items)
            {
                yield return item;
            }
        }

        throw new InvalidOperationException(
            $"Pagination did not complete within {maxPages} pages. Raise maxPages if the collection is genuinely that large, or verify that the API returns an empty page at the end.");
    }
}
