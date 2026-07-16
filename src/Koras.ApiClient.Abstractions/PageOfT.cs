namespace Koras.ApiClient;

/// <summary>
/// One page of items extracted from a paginated API response, used by the pagination extensions of
/// the <c>Koras.ApiClient</c> package.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items on this page. An empty list with a <see langword="null"/> cursor ends enumeration.</param>
/// <param name="NextCursor">
/// The opaque cursor identifying the next page, or <see langword="null"/>/empty when this is the last page.
/// </param>
/// <param name="TotalCount">The total item count across all pages, when the API reports one.</param>
public sealed record Page<T>(IReadOnlyList<T> Items, string? NextCursor = null, long? TotalCount = null);
