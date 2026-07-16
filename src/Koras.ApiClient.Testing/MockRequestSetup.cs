using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Koras.ApiClient.Testing;

/// <summary>
/// Scripts the responses for one request pattern. Chain multiple <c>RespondWith*</c>/<c>Throws</c>
/// calls to build a sequence; the last scripted response repeats for subsequent calls.
/// </summary>
public sealed class MockRequestSetup
{
    private readonly object _syncRoot = new();
    private readonly List<Func<HttpResponseMessage>> _responses = [];
    private int _callCount;

    internal MockRequestSetup(HttpMethod method, string pathPattern)
    {
        Method = method;
        PathPattern = pathPattern.StartsWith('/') ? pathPattern : "/" + pathPattern;
    }

    internal HttpMethod Method { get; }

    internal string PathPattern { get; }

    /// <summary>Scripts a response with the given status and optional text body.</summary>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="body">The optional response body text.</param>
    /// <param name="mediaType">The body media type. Defaults to <c>application/json</c>.</param>
    /// <param name="configure">Optional response customization (headers such as <c>Retry-After</c>).</param>
    /// <returns>This setup, for scripting further responses in sequence.</returns>
    public MockRequestSetup RespondWith(HttpStatusCode statusCode, string? body = null, string mediaType = "application/json", Action<HttpResponseMessage>? configure = null)
    {
        return RespondWith(() =>
        {
            var response = new HttpResponseMessage(statusCode);
            if (body is not null)
            {
                response.Content = new StringContent(body, Encoding.UTF8, mediaType);
            }

            configure?.Invoke(response);
            return response;
        });
    }

    /// <summary>Scripts a response whose body is <paramref name="body"/> serialized as JSON.</summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="body">The value to serialize.</param>
    /// <param name="statusCode">The response status code. Defaults to 200 OK.</param>
    /// <returns>This setup, for scripting further responses in sequence.</returns>
    public MockRequestSetup RespondWithJson<T>(T body, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        RespondWith(() => new HttpResponseMessage(statusCode) { Content = JsonContent.Create(body) });

    /// <summary>Scripts an RFC 9457 <c>application/problem+json</c> response.</summary>
    /// <param name="problem">The problem document to serialize.</param>
    /// <param name="statusCode">The response status code. Defaults to 400 Bad Request.</param>
    /// <returns>This setup, for scripting further responses in sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problem"/> is <see langword="null"/>.</exception>
    public MockRequestSetup RespondWithProblem(ProblemDocument problem, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        ArgumentNullException.ThrowIfNull(problem);
        return RespondWith(() => new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(problem, new MediaTypeHeaderValue("application/problem+json")),
        });
    }

    /// <summary>Scripts a response produced by a factory — full control over status, headers, and content.</summary>
    /// <param name="responseFactory">Creates a fresh response for each matching request.</param>
    /// <returns>This setup, for scripting further responses in sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="responseFactory"/> is <see langword="null"/>.</exception>
    public MockRequestSetup RespondWith(Func<HttpResponseMessage> responseFactory)
    {
        ArgumentNullException.ThrowIfNull(responseFactory);
        lock (_syncRoot)
        {
            _responses.Add(responseFactory);
        }

        return this;
    }

    /// <summary>Scripts a thrown exception, simulating transport failures (for example <see cref="HttpRequestException"/>).</summary>
    /// <param name="exception">The exception to throw when the request matches.</param>
    /// <returns>This setup, for scripting further responses in sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public MockRequestSetup Throws(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return RespondWith(new Func<HttpResponseMessage>(() => throw exception));
    }

    internal bool Matches(HttpRequestMessage request)
    {
        if (request.Method != Method || request.RequestUri is not { } uri)
        {
            return false;
        }

        var target = PathPattern.Contains('?') ? uri.PathAndQuery : uri.AbsolutePath;
        if (PathPattern.EndsWith('*'))
        {
            return target.StartsWith(PathPattern[..^1], StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(target, PathPattern, StringComparison.OrdinalIgnoreCase);
    }

    internal HttpResponseMessage CreateResponse()
    {
        Func<HttpResponseMessage>? factory;
        lock (_syncRoot)
        {
            if (_responses.Count == 0)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            factory = _responses[Math.Min(_callCount, _responses.Count - 1)];
            _callCount++;
        }

        return factory();
    }
}
