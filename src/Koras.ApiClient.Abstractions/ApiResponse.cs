using System.Net;

namespace Koras.ApiClient;

/// <summary>
/// The outcome of an API operation without a deserialized body. Inspect <see cref="IsSuccess"/> and
/// <see cref="Error"/>, or call <see cref="EnsureSuccess"/> to convert failures into
/// <see cref="ApiClientException"/>.
/// </summary>
public class ApiResponse
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyHeaders =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>The HTTP status code, or <see langword="null"/> when no response was received (transport failures).</summary>
    public HttpStatusCode? StatusCode { get; init; }

    /// <summary>The normalized error, or <see langword="null"/> when the operation succeeded.</summary>
    public ApiError? Error { get; init; }

    /// <summary>The response headers (response and content headers combined, case-insensitive names).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; init; } = EmptyHeaders;

    /// <summary>The correlation identifier that was sent with the request, when correlation is enabled.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Indicates whether the operation succeeded (<see cref="Error"/> is <see langword="null"/>).</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Throws an <see cref="ApiClientException"/> when the operation failed; otherwise does nothing.</summary>
    /// <exception cref="ApiClientException">The operation failed.</exception>
    public void EnsureSuccess()
    {
        if (Error is { } error)
        {
            throw new ApiClientException(error);
        }
    }
}
