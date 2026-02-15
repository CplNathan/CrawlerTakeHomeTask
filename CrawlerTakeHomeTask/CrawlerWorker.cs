namespace CrawlerTakeHomeTask;

using Microsoft.Extensions.Options;
using CrawlerTakeHomeTask.Models;
using CrawlerTakeHomeTask.Services.Cache;
using CrawlerTakeHomeTask.Services.Channels;
using CrawlerTakeHomeTask.Services.Crawler;
using System.Diagnostics;

public class CrawlerWorker(ICrawlerFetcher fetcher, ICrawlerParser parser, ICrawlerChannel channel, ICrawlerCache cache, IHostApplicationLifetime lifetime, ILogger<CrawlerWorker> logger, IOptions<CrawlerConfig> crawlerOptions) : BackgroundService
{
    private readonly CrawlerConfig crawlerConfig = crawlerOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        await channel.Write(new PageModel(crawlerConfig.BaseUrl), token);

        var timer = new Stopwatch();

        timer.Start();
        await Task.WhenAll([.. Enumerable.Range(0, crawlerConfig.MaxParallelism).Select(_ => RunWorker(token))]);
        timer.Stop();

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Crawl completed in {elapsedSeconds} seconds visited {pageCount} pages", timer.Elapsed.TotalSeconds, await cache.StoredItems());

        lifetime.StopApplication();
    }

    private async Task RunWorker(CancellationToken token)
    {
        while (await LinkedWaitToRead(token))
        {
            await cache.IncrementWorkers().ConfigureAwait(false);

            try
            {
                if (!channel.Items.TryRead(out PageModel? parentPage) || parentPage is null) continue;

                using var htmlDocument = await fetcher.GetPage(parentPage.Url, token);
                if (htmlDocument is null) continue;

                var discoveredPages = parser.DiscoverUrls(htmlDocument, parentPage.Url, parentPage.Depth, parentPage.Url, token).ConfigureAwait(false).WithCancellation(token);
                await foreach (var childPage in discoveredPages)
                {
                    if (!await cache.SetAdd(childPage.GetHashCode().ToString()).ConfigureAwait(false)) continue;

                    await channel.Write(childPage, token).ConfigureAwait(false);
                }
            }
            finally
            {
                var workers = await cache.DecrementWorkers().ConfigureAwait(false);
                if (workers == 0 && await channel.Length() == 0)
                    await channel.TryComplete();
            }
        }
        ;
    }

    private async Task<bool> LinkedWaitToRead(CancellationToken token)
    {
        var timedToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token, timedToken.Token);

        try
        {
            return await channel.Items.WaitToReadAsync(cts.Token).ConfigureAwait(false);
        }
        catch
        {
            if (await cache.Workers() == 0 && await channel.Length() == 0)
                await channel.TryComplete();
        }

        return channel.Items.Completion != Task.CompletedTask;
    }
}
