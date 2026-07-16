using System.Net;
using System.Text.Json;

namespace Koras.ApiClient;

/// <summary>Normalizes every failure mode into an <see cref="ApiError"/>. Single source of truth for the error taxonomy.</summary>
internal static class ApiErrorFactory
{
    private const long MaxBodyReadBytes = 1_048_576;

    private static readonly JsonSerializerOptions ProblemJsonOptions = new(JsonSerializerDefaults.Web);

    public static ApiError FromNetworkError(Exception exception) => new()
    {
        Category = ApiErrorCategory.Network,
        Code = "network_error",
        Message = "The request failed due to a connection error. See the Exception property for details.",
        Exception = exception,
    };

    public static ApiError FromTimeout(Exception exception) => new()
    {
        Category = ApiErrorCategory.Timeout,
        Code = "timeout",
        Message = "The request did not complete within the allotted timeout.",
        Exception = exception,
    };

    public static ApiError FromDeserialization(Exception exception, HttpStatusCode statusCode) => new()
    {
        Category = ApiErrorCategory.Serialization,
        Code = "deserialization_failed",
        Message = "The response body could not be deserialized into the requested type.",
        StatusCode = statusCode,
        Exception = exception,
    };

    public static async Task<ApiError> FromResponseAsync(
        HttpResponseMessage response,
        KorasApiClientOptions options,
        CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        var body = await CaptureBodyAsync(response, options, cancellationToken).ConfigureAwait(false);
        var problem = TryParseProblem(response, body);

        var message = problem?.Title ?? problem?.Detail;
        if (string.IsNullOrEmpty(message))
        {
            message = string.IsNullOrEmpty(response.ReasonPhrase)
                ? $"The API returned HTTP {statusCode}."
                : $"The API returned HTTP {statusCode} ({response.ReasonPhrase}).";
        }

        return new ApiError
        {
            Category = MapCategory(statusCode),
            Code = $"http_{statusCode}",
            Message = message,
            StatusCode = response.StatusCode,
            Problem = problem,
            ResponseBody = body,
            RetryAfter = GetRetryAfter(response),
        };
    }

    private static async Task<string?> CaptureBodyAsync(
        HttpResponseMessage response,
        KorasApiClientOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.CaptureErrorResponseBody
            || options.MaxErrorResponseBodyLength <= 0
            || response.Content.Headers.ContentLength is > MaxBodyReadBytes)
        {
            return null;
        }

        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (body.Length > options.MaxErrorResponseBodyLength)
            {
                body = body[..options.MaxErrorResponseBodyLength];
            }

            return body.Length == 0 ? null : body;
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException or InvalidOperationException)
        {
            return null;
        }
    }

    private static ProblemDocument? TryParseProblem(HttpResponseMessage response, string? body)
    {
        if (body is null)
        {
            return null;
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is null || !mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var problem = JsonSerializer.Deserialize<ProblemDocument>(body, ProblemJsonOptions);
            if (problem is null)
            {
                return null;
            }

            var hasProblemMembers = problem.Type is not null
                || problem.Title is not null
                || problem.Status is not null
                || problem.Detail is not null
                || problem.Errors is not null;
            return hasProblemMembers ? problem : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ApiErrorCategory MapCategory(int statusCode) => statusCode switch
    {
        400 or 422 => ApiErrorCategory.Validation,
        401 => ApiErrorCategory.Authentication,
        403 => ApiErrorCategory.Authorization,
        404 or 410 => ApiErrorCategory.NotFound,
        408 => ApiErrorCategory.Timeout,
        409 or 412 => ApiErrorCategory.Conflict,
        429 => ApiErrorCategory.RateLimited,
        >= 400 and < 500 => ApiErrorCategory.Client,
        >= 500 => ApiErrorCategory.Server,
        _ => ApiErrorCategory.Unknown,
    };

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter is null)
        {
            return null;
        }

        if (retryAfter.Delta is { } delta)
        {
            return delta >= TimeSpan.Zero ? delta : TimeSpan.Zero;
        }

        if (retryAfter.Date is { } date)
        {
            var delay = date - DateTimeOffset.UtcNow;
            return delay >= TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return null;
    }
}
