using Microsoft.Extensions.Options;
using CrawlerTakeHomeTask;
using CrawlerTakeHomeTask.Models;
using CrawlerTakeHomeTask.Services.Cache;
using CrawlerTakeHomeTask.Services.Channels;
using CrawlerTakeHomeTask.Services.Crawler;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Threading.Channels;

internal class Program
{
    public class HttpClientCrawlerHandler : DelegatingHandler
    {
        public HttpClientCrawlerHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }
    }

    public const string CrawlerClientName = "CrawlerClient";

    public const string CrawlUrlKey = "CrawlUrl";
    public const string CrawlerConfigKey = "CrawlerConfig";

    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        if (builder.Configuration.GetValue<bool>("Distributed"))
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq" };
            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            var distributedChannel = new DistributedChannel(channel);
            await distributedChannel.Hookup(CancellationToken.None);

            builder.Services.AddSingleton<ICrawlerChannel>(distributedChannel);

            builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("redis"));
            builder.Services.AddScoped(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

            builder.Services.AddSingleton<ICrawlerCache, DistributedCache>();
        }
        else
        {
            var channel = Channel.CreateUnbounded<PageModel>();

            var localChannel = new LocalChannel(channel);
            await localChannel.Hookup(CancellationToken.None);

            builder.Services.AddSingleton<ICrawlerChannel>(localChannel);

            builder.Services.AddSingleton<ICrawlerCache, MemoryCache>();
        }
        
        builder.Services.AddHostedService<CrawlerWorker>();
        builder.Services.AddScoped<ICrawlerFetcher, CrawlerFetcher>();
        builder.Services.AddScoped<ICrawlerParser, CrawlerParser>();

        var rootAppSettings = builder.Services
            .AddOptions<CrawlerConfig>()
            .Bind((builder.Configuration.GetSection(CrawlerConfigKey)))
            .ValidateOnStart();

        static void ConfigureHttpClient(IServiceProvider serviceProvider, HttpClient client)
        {
            var crawlerConfig = serviceProvider.GetRequiredService<IOptions<CrawlerConfig>>().Value;

            client.BaseAddress = crawlerConfig.BaseUrl;
            client.Timeout = crawlerConfig.RequestTimeout;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(crawlerConfig.UserAgent);
        }

        static DelegatingHandler ConfigureHttpHandler(IServiceProvider serviceProvider)
        {
            var crawlerConfig = serviceProvider.GetRequiredService<IOptions<CrawlerConfig>>().Value;

            var handler = new SocketsHttpHandler
            {
                MaxConnectionsPerServer = crawlerConfig.MaxParallelism,
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                ConnectTimeout = crawlerConfig.RequestTimeout,
                AllowAutoRedirect = crawlerConfig.FollowRedirects,
            };

            return new HttpClientCrawlerHandler(handler);
        }

        builder.Services
            .AddHttpClient(
                CrawlerClientName,
                ConfigureHttpClient)
            .ConfigurePrimaryHttpMessageHandler(
                ConfigureHttpHandler
            );

        var host = builder.Build();

        host.Run();
    }
}