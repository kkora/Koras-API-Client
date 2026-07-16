namespace Koras.ApiClient;

/// <summary>
/// The outcome of an API operation whose successful response body is deserialized as
/// <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type the response body is deserialized into.</typeparam>
public sealed class ApiResponse<T> : ApiResponse
{
    /// <summary>
    /// The deserialized response body. <see langword="null"/> (or <see langword="default"/>) when
    /// the operation failed, or when a successful response carried no body (for example HTTP 204).
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Returns <see cref="Value"/>, throwing when the operation failed or produced no value.
    /// </summary>
    /// <returns>The non-null deserialized value.</returns>
    /// <exception cref="ApiClientException">The operation failed, or succeeded without a body.</exception>
    public T GetValueOrThrow()
    {
        EnsureSuccess();
        return Value ?? throw new ApiClientException(new ApiError
        {
            Category = ApiErrorCategory.Serialization,
            Code = "empty_response",
            Message = "The operation succeeded but the response contained no value.",
            StatusCode = StatusCode,
        });
    }

    /// <summary>Projects the outcome into a single result by invoking exactly one of the two functions.</summary>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="onSuccess">Invoked with <see cref="Value"/> when the operation succeeded (the value may be <see langword="null"/> for bodiless successes).</param>
    /// <param name="onFailure">Invoked with the <see cref="ApiError"/> when the operation failed.</param>
    /// <returns>The value produced by the invoked function.</returns>
    /// <exception cref="ArgumentNullException">Either function is <see langword="null"/>.</exception>
    public TResult Match<TResult>(Func<T?, TResult> onSuccess, Func<ApiError, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return Error is { } error ? onFailure(error) : onSuccess(Value);
    }
}
