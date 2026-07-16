namespace Koras.ApiClient;

/// <summary>
/// The single exception type thrown when an <see cref="ApiError"/> is converted to an exception,
/// for example by <see cref="ApiResponse.EnsureSuccess"/> or <see cref="ApiResponse{T}.GetValueOrThrow"/>.
/// </summary>
public sealed class ApiClientException : Exception
{
    /// <summary>Initializes a new instance carrying the specified normalized error.</summary>
    /// <param name="error">The normalized error describing the failure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public ApiClientException(ApiError error)
        : base(error?.Message ?? "The API request failed.", error?.Exception)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    /// <summary>The normalized error that caused this exception.</summary>
    public ApiError Error { get; }
}
