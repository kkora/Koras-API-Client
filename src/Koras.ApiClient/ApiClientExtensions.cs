namespace Koras.ApiClient;

/// <summary>Fluent entry points for composing requests on any <see cref="IApiClient"/>.</summary>
public static class ApiClientExtensions
{
    /// <summary>Begins a GET request to <paramref name="path"/>.</summary>
    /// <param name="client">The API client.</param>
    /// <param name="path">The request path, absolute URL, or path-and-query.</param>
    /// <returns>A request builder.</returns>
    public static ApiRequestBuilder Get(this IApiClient client, string path) => Create(client, HttpMethod.Get, path);

    /// <summary>Begins a POST request to <paramref name="path"/>.</summary>
    /// <param name="client">The API client.</param>
    /// <param name="path">The request path, absolute URL, or path-and-query.</param>
    /// <returns>A request builder.</returns>
    public static ApiRequestBuilder Post(this IApiClient client, string path) => Create(client, HttpMethod.Post, path);

    /// <summary>Begins a PUT request to <paramref name="path"/>.</summary>
    /// <param name="client">The API client.</param>
    /// <param name="path">The request path, absolute URL, or path-and-query.</param>
    /// <returns>A request builder.</returns>
    public static ApiRequestBuilder Put(this IApiClient client, string path) => Create(client, HttpMethod.Put, path);

    /// <summary>Begins a PATCH request to <paramref name="path"/>.</summary>
    /// <param name="client">The API client.</param>
    /// <param name="path">The request path, absolute URL, or path-and-query.</param>
    /// <returns>A request builder.</returns>
    public static ApiRequestBuilder Patch(this IApiClient client, string path) => Create(client, HttpMethod.Patch, path);

    /// <summary>Begins a DELETE request to <paramref name="path"/>.</summary>
    /// <param name="client">The API client.</param>
    /// <param name="path">The request path, absolute URL, or path-and-query.</param>
    /// <returns>A request builder.</returns>
    public static ApiRequestBuilder Delete(this IApiClient client, string path) => Create(client, HttpMethod.Delete, path);

    /// <summary>Begins a HEAD request to <paramref name="path"/>.</summary>
    /// <param name="client">The API client.</param>
    /// <param name="path">The request path, absolute URL, or path-and-query.</param>
    /// <returns>A request builder.</returns>
    public static ApiRequestBuilder Head(this IApiClient client, string path) => Create(client, HttpMethod.Head, path);

    /// <summary>Sends a GET request and deserializes the response body as <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The response body type.</typeparam>
    /// <param name="client">The API client.</param>
    /// <param name="path">The request path, absolute URL, or path-and-query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The normalized outcome carrying the value on success.</returns>
    public static Task<ApiResponse<T>> GetFromJsonAsync<T>(this IApiClient client, string path, CancellationToken cancellationToken = default) =>
        client.Get(path).SendAsync<T>(cancellationToken);

    /// <summary>Sends a POST request with a JSON body and deserializes the response body as <typeparamref name="TResponse"/>.</summary>
    /// <typeparam name="TRequest">The request body type.</typeparam>
    /// <typeparam name="TResponse">The response body type.</typeparam>
    /// <param name="client">The API client.</param>
    /// <param name="path">The request path, absolute URL, or path-and-query.</param>
    /// <param name="body">The request body to serialize as JSON.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The normalized outcome carrying the value on success.</returns>
    public static Task<ApiResponse<TResponse>> PostJsonAsync<TRequest, TResponse>(this IApiClient client, string path, TRequest body, CancellationToken cancellationToken = default) =>
        client.Post(path).WithJsonBody(body).SendAsync<TResponse>(cancellationToken);

    private static ApiRequestBuilder Create(IApiClient client, HttpMethod method, string path)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(path);
        return new ApiRequestBuilder(client, method, path);
    }
}
