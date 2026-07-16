namespace Koras.ApiClient;

/// <summary>
/// Well-known telemetry identifiers emitted by Koras.ApiClient. Register these with your
/// OpenTelemetry pipeline via <c>AddSource</c> and <c>AddMeter</c>.
/// </summary>
public static class KorasApiClientTelemetry
{
    /// <summary>The name of the <see cref="System.Diagnostics.ActivitySource"/> that emits one span per logical API operation.</summary>
    public const string ActivitySourceName = "Koras.ApiClient";

    /// <summary>The name of the <see cref="System.Diagnostics.Metrics.Meter"/> that emits request count and duration instruments.</summary>
    public const string MeterName = "Koras.ApiClient";
}
