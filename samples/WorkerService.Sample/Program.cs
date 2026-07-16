// Worker Service sample: a background poller consuming a paginated API as IAsyncEnumerable<T>
// with graceful cancellation on shutdown.
using Koras.ApiClient;
using Koras.ApiClient.Samples.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddKorasApiClient("placeholder", options =>
{
    options.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
    options.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHostedService<PostsPoller>();

await builder.Build().RunAsync();
