using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Koras.ApiClient.Samples.Worker;

internal sealed record Post(int Id, int UserId, string Title, string Body);

/// <summary>Polls a page-number-paginated API every 30 seconds, enumerating all pages as a stream.</summary>
internal sealed class PostsPoller(IApiClientFactory clientFactory, ILogger<PostsPoller> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = clientFactory.CreateClient("placeholder");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var count = 0;
                await foreach (var post in client.PaginateByPageNumberAsync<List<Post>, Post>(
                    "posts",
                    page => page,
                    pageParameterName: "_page",
                    configureRequest: request => request.WithQuery("_limit", 25),
                    cancellationToken: stoppingToken))
                {
                    count++;
                }

                logger.LogInformation("Fetched {PostCount} posts across all pages.", count);
            }
            catch (ApiClientException exception)
            {
                // Pagination surfaces failed pages as ApiClientException so partial data is never mistaken for complete data.
                logger.LogWarning(
                    "Polling failed with {Category}/{Code}; retrying next cycle.",
                    exception.Error.Category,
                    exception.Error.Code);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
