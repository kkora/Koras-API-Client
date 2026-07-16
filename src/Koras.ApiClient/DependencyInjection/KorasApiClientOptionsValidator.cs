using Microsoft.Extensions.Options;

namespace Koras.ApiClient;

/// <summary>Validates <see cref="KorasApiClientOptions"/> at startup so misconfiguration fails fast.</summary>
internal sealed class KorasApiClientOptionsValidator : IValidateOptions<KorasApiClientOptions>
{
    public ValidateOptionsResult Validate(string? name, KorasApiClientOptions options)
    {
        List<string>? failures = null;

        if (options.Timeout <= TimeSpan.Zero)
        {
            Add(ref failures, "Timeout must be positive.");
        }

        if (options.BaseAddress is { IsAbsoluteUri: false })
        {
            Add(ref failures, "BaseAddress must be an absolute URI.");
        }

        if (options.EnableCorrelationId && string.IsNullOrWhiteSpace(options.CorrelationHeaderName))
        {
            Add(ref failures, "CorrelationHeaderName must be set when EnableCorrelationId is true.");
        }

        if (options.MaxErrorResponseBodyLength < 0)
        {
            Add(ref failures, "MaxErrorResponseBodyLength must not be negative.");
        }

        if (options.Caching.Enabled && options.Caching.DefaultDuration <= TimeSpan.Zero)
        {
            Add(ref failures, "Caching.DefaultDuration must be positive when caching is enabled.");
        }

        if (options.Caching.MaxEntrySizeBytes < 0)
        {
            Add(ref failures, "Caching.MaxEntrySizeBytes must not be negative.");
        }

        return failures is null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Add(ref List<string>? failures, string message)
    {
        failures ??= [];
        failures.Add(message);
    }
}
