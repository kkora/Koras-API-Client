using System.Text.Json;

namespace Koras.ApiClient;

/// <summary>
/// A named, thread-safe API client. Failures are returned as data on <see cref="ApiResponse"/>
/// rather than thrown: only caller cancellation (<see cref="OperationCanceledException"/>) and
/// programmer errors (argument/state exceptions) escape these methods.
/// </summary>
public interface IApiClient
{
    /// <summary>The logical name of this client (used for options, logging, and telemetry tags).</summary>
    string Name { get; }

    /// <summary>The JSON serializer options used for request and response bodies.</summary>
    JsonSerializerOptions JsonOptions { get; }

    /// <summary>Sends the request and returns the outcome without deserializing the response body.</summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The normalized outcome of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The caller's token was cancelled.</exception>
    Task<ApiResponse> SendAsync(ApiRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sends the request and deserializes a successful response body as <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">
    /// The response body type. Use <see cref="string"/> to receive the raw body text without JSON parsing.
    /// </typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The normalized outcome carrying the deserialized value on success.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The caller's token was cancelled.</exception>
    Task<ApiResponse<T>> SendAsync<T>(ApiRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the request and streams a successful response body into <paramref name="destination"/>
    /// without buffering it in memory.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="destination">The writable stream that receives the response body.</param>
    /// <param name="progress">Optional progress callback reporting total bytes copied so far.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The normalized outcome of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The caller's token was cancelled.</exception>
    Task<ApiResponse> DownloadAsync(ApiRequest request, Stream destination, IProgress<long>? progress = null, CancellationToken cancellationToken = default);
}
