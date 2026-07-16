# Your First Application

A complete console application from zero:

```bash
dotnet new console -n MyFirstKoras && cd MyFirstKoras
dotnet add package Koras.ApiClient
dotnet add package Microsoft.Extensions.Hosting
```

`Program.cs`:

```csharp
using Koras.ApiClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddKorasApiClient(options =>
    options.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/"));

using var host = builder.Build();
var client = host.Services.GetRequiredService<IApiClient>();

var response = await client.Get("todos/1").SendAsync<Todo>();
Console.WriteLine(response.Match(
    todo => $"Fetched: {todo!.Title}",
    error => $"Failed: {error.Category} — {error.Message}"));

internal sealed record Todo(int Id, int UserId, string Title, bool Completed);
```

```bash
dotnet run
# Fetched: delectus aut autem
```

Pull the network cable and run again — you get `Failed: Network — The request failed due to a connection error…` after bounded retries, not a stack trace. That's the whole philosophy in one demo.

Next: the [Console sample](../../samples/Console.Sample/) adds caching, POSTs, correlation IDs, and an offline mock mode.
