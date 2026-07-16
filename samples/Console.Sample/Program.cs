// Console sample for Koras.ApiClient.
// Run online against jsonplaceholder.typicode.com:   dotnet run
// Run fully offline against a scripted mock:          dotnet run -- --offline
using Koras.ApiClient;
using Koras.ApiClient.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var offline = args.Contains("--offline");

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Information);

var clientBuilder = builder.Services.AddKorasApiClient(options =>
{
    options.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
    options.Timeout = TimeSpan.FromSeconds(15);
});

if (offline)
{
    // The Testing package swaps the transport without touching any application code.
    var mock = new MockApiHandler();
    mock.When(HttpMethod.Get, "/todos/1").RespondWithJson(new Todo(1, 1, "offline todo from the mock", false));
    mock.When(HttpMethod.Post, "/todos").RespondWithJson(new Todo(201, 1, "created offline", false), System.Net.HttpStatusCode.Created);
    clientBuilder.HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => mock);
    Console.WriteLine("Running in offline mode over MockApiHandler.\n");
}

using var host = builder.Build();
var client = host.Services.GetRequiredService<IApiClient>();

// 1. Typed GET — success is a value, failure is a categorized ApiError. Never surprise exceptions.
var todo = await client.Get("todos/1").SendAsync<Todo>();
Console.WriteLine(todo.Match(
    value => $"1) GET todos/1        -> #{value!.Id} \"{value.Title}\"",
    error => $"1) GET todos/1        -> {error.Category}: {error.Message}"));

// 2. Errors as data — a 404 is just a NotFound category, not an exception.
var missing = await client.Get("todos/999999").SendAsync<Todo>();
Console.WriteLine($"2) GET todos/999999   -> {missing.Error?.Category} ({missing.Error?.Code}) as expected");

// 3. POST with a JSON body.
var created = await client.Post("todos").WithJsonBody(new Todo(0, 1, "learn Koras.ApiClient", false)).SendAsync<Todo>();
Console.WriteLine(created.Match(
    value => $"3) POST todos         -> created #{value!.Id}",
    error => $"3) POST todos         -> {error.Category}: {error.Message}"));

// 4. Response caching — the second call is served from memory (watch the logs: only one HTTP request).
await client.Get("todos/1").WithCache(TimeSpan.FromMinutes(1)).SendAsync<Todo>();
var cached = await client.Get("todos/1").WithCache(TimeSpan.FromMinutes(1)).SendAsync<Todo>();
Console.WriteLine($"4) Cached GET         -> served again: \"{cached.Value?.Title}\" (no second HTTP request)");

// 5. Correlation IDs flow automatically and are echoed on every response.
Console.WriteLine($"5) Correlation ID     -> {cached.CorrelationId}");

internal sealed record Todo(int Id, int UserId, string Title, bool Completed);
