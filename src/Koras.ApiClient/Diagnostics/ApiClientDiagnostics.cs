using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Koras.ApiClient;

/// <summary>Shared telemetry instruments for the client (internal; names published via <see cref="KorasApiClientTelemetry"/>).</summary>
internal static class ApiClientDiagnostics
{
    private static readonly string Version =
        typeof(ApiClientDiagnostics).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public static readonly ActivitySource ActivitySource = new(KorasApiClientTelemetry.ActivitySourceName, Version);

    public static readonly Meter Meter = new(KorasApiClientTelemetry.MeterName, Version);

    public static readonly Counter<long> Requests = Meter.CreateCounter<long>(
        "koras.apiclient.requests",
        unit: "{request}",
        description: "Number of logical API requests sent (retries count as one request).");

    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "koras.apiclient.request.duration",
        unit: "ms",
        description: "Duration of logical API requests including retries.");
}
