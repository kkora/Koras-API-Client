using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using Koras.ApiClient.Testing;
using Koras.ApiClient.UnitTests.Support;
using Xunit;

namespace Koras.ApiClient.UnitTests;

public class TelemetryTests
{
    [Fact]
    public async Task SendAsync_EmitsActivityWithTags()
    {
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == KorasApiClientTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (activities)
                {
                    activities.Add(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/traced").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler, name: "TraceClient");

        await client.Get("traced").SendAsync();

        List<Activity> snapshot;
        lock (activities)
        {
            snapshot = [.. activities];
        }

        var activity = Assert.Single(snapshot, a => Equals(a.GetTagItem("koras.client.name"), "TraceClient"));
        Assert.Equal("GET TraceClient", activity.DisplayName);
        Assert.Equal("GET", activity.GetTagItem("http.request.method"));
        Assert.Equal("traced", activity.GetTagItem("url.path"));
        Assert.Equal(200, activity.GetTagItem("http.response.status_code"));
    }

    [Fact]
    public async Task FailedRequest_MarksActivityAsError()
    {
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == KorasApiClientTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (activities)
                {
                    activities.Add(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/fail").RespondWith(HttpStatusCode.InternalServerError);
        var client = KorasApiClientTestFactory.Create(handler, name: "TraceErrClient");

        await client.Get("fail").SendAsync();

        List<Activity> snapshot;
        lock (activities)
        {
            snapshot = [.. activities];
        }

        var activity = Assert.Single(snapshot, a => Equals(a.GetTagItem("koras.client.name"), "TraceErrClient"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("http_500", activity.GetTagItem("error.type"));
    }

    [Fact]
    public async Task SendAsync_RecordsRequestCounterAndDuration()
    {
        var measurements = new List<KeyValuePair<string, object?>[]>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == KorasApiClientTelemetry.MeterName && instrument.Name == "koras.apiclient.requests")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            lock (measurements)
            {
                measurements.Add(tags.ToArray());
            }
        });
        meterListener.Start();

        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/metered").RespondWith(HttpStatusCode.OK);
        var client = KorasApiClientTestFactory.Create(handler, name: "MeterClient");

        await client.Get("metered").SendAsync();

        KeyValuePair<string, object?>[]? tags;
        lock (measurements)
        {
            tags = measurements.FirstOrDefault(m => m.Any(t => t.Key == "koras.client.name" && Equals(t.Value, "MeterClient")));
        }

        Assert.NotNull(tags);
        Assert.Contains(tags!, t => t.Key == "http.request.method" && Equals(t.Value, "GET"));
        Assert.Contains(tags!, t => t.Key == "http.response.status_code" && Equals(t.Value, 200));
    }

    [Fact]
    public async Task Logging_NeverIncludesQueryStringsOrCredentials()
    {
        var logger = new TestLogger<KorasApiClient>();
        var handler = new MockApiHandler();
        handler.When(HttpMethod.Get, "/resource*").RespondWith(HttpStatusCode.OK);
        var options = new KorasApiClientOptions { BaseAddress = new Uri("https://mock.local/") };
        var client = new KorasApiClient("LogClient", new HttpClient(handler) { BaseAddress = options.BaseAddress }, options, logger);

        await client.Get("resource?api_key=super-secret-value")
            .WithHeader("Authorization", "Bearer super-secret-token")
            .SendAsync();

        Assert.NotEmpty(logger.Messages);
        Assert.All(logger.Messages, message =>
        {
            Assert.DoesNotContain("super-secret-value", message);
            Assert.DoesNotContain("super-secret-token", message);
            Assert.DoesNotContain("api_key", message);
        });
        Assert.Contains(logger.Messages, message => message.Contains("resource") && message.Contains("LogClient"));
    }
}
