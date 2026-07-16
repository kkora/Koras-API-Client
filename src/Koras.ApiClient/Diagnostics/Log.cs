using Microsoft.Extensions.Logging;

namespace Koras.ApiClient;

/// <summary>
/// Source-generated structured log messages. Paths are logged without query strings and no
/// credential-bearing header or token value is ever passed to these messages.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug,
        Message = "Sending {Method} {Path} on client {ClientName} (correlation: {CorrelationId})")]
    public static partial void RequestStarting(ILogger logger, string method, string path, string clientName, string? correlationId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "{Method} {Path} on client {ClientName} completed with status {StatusCode} in {ElapsedMilliseconds}ms (correlation: {CorrelationId})")]
    public static partial void RequestCompleted(ILogger logger, string method, string path, string clientName, int statusCode, double elapsedMilliseconds, string? correlationId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning,
        Message = "{Method} {Path} on client {ClientName} returned non-success status {StatusCode} in {ElapsedMilliseconds}ms (correlation: {CorrelationId})")]
    public static partial void RequestNonSuccess(ILogger logger, string method, string path, string clientName, int statusCode, double elapsedMilliseconds, string? correlationId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning,
        Message = "{Method} {Path} on client {ClientName} failed with {ErrorCategory}/{ErrorCode} in {ElapsedMilliseconds}ms (correlation: {CorrelationId})")]
    public static partial void RequestFailed(ILogger logger, string method, string path, string clientName, string errorCategory, string errorCode, double elapsedMilliseconds, string? correlationId);
}
