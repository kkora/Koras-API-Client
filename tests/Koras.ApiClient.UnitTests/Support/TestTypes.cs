using Microsoft.Extensions.Logging;

namespace Koras.ApiClient.UnitTests.Support;

public sealed record Widget(int Id, string Name);

public sealed record CursorPage(IReadOnlyList<Widget> Items, string? Next);

public sealed record NumberedPage(IReadOnlyList<Widget> Items);

internal sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _utcNow = start;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan delta) => _utcNow += delta;
}

internal sealed class TestLogger<T> : ILogger<T>
{
    private readonly object _syncRoot = new();
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages
    {
        get
        {
            lock (_syncRoot)
            {
                return [.. _messages];
            }
        }
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_syncRoot)
        {
            _messages.Add(formatter(state, exception));
        }
    }
}

internal sealed class DelayHandler(TimeSpan delay) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
    }
}
