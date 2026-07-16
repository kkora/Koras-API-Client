namespace Koras.ApiClient.Testing;

/// <summary>A snapshot of one request received by <see cref="MockApiHandler"/>.</summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Uri">The absolute request URI.</param>
/// <param name="Body">The request body read as text, or <see langword="null"/> for bodiless requests.</param>
/// <param name="Headers">The request headers (request and content headers combined).</param>
public sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    string? Body,
    IReadOnlyDictionary<string, string[]> Headers);
