namespace CrawlerTakeHomeTask.UnitTests;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CrawlerTakeHomeTask.Models;
using CrawlerTakeHomeTask.Services.Crawler;
using NSubstitute;

[TestClass]
public class CrawlerServiceTests
{
    private CrawlerConfig GetDefaultConfig()
    {
        return new CrawlerConfig
        {
            BaseUrl = new Uri("http://localhost:8080"),
            MaxDepth = 5,
            MaxParallelism = 3,
            RequestTimeout = TimeSpan.FromSeconds(5),
            UserAgent = "Test-Crawler/1.0",
            FollowRedirects = false
        };
    }

    // Arrange
    [TestMethod]
    public void Constructor_InitializesWithValidDependencies()
    {
        // Arrange
        var mockHttpClientFactory = Substitute.For<IHttpClientFactory>();
        var mockHttpClient = new HttpClient();
        mockHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(mockHttpClient);

        var mockOptions = Options.Create(GetDefaultConfig());
        var mockLogger = Substitute.For<ILogger<CrawlerFetcher>>();

        // Act
        var service = new CrawlerService(mockHttpClientFactory, mockOptions, mockLogger);

        // Assert
        Assert.IsNotNull(service);
        Assert.IsTrue(service.CanStartCrawling);
    }

    [TestMethod]
    public async Task StartCrawling_ThrowsInvalidOperationException_WhenCrawlAlreadyInProgress()
    {
        // Arrange
        var mockHttpClientFactory = Substitute.For<IHttpClientFactory>();
        var mockHttpClient = new HttpClient();
        mockHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(mockHttpClient);

        var mockOptions = Options.Create(GetDefaultConfig());
        var mockLogger = Substitute.For<ILogger<CrawlerFetcher>>();

        var service = new CrawlerService(mockHttpClientFactory, mockOptions, mockLogger);
        var startUri = new Uri("http://localhost:8080/index.html");
        var cts = new CancellationTokenSource();

        // Act & Assert
        // First crawl starts
        var crawlTask = service.StartCrawling(startUri, cts.Token);

        // Second crawl should throw
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.StartCrawling(startUri, cts.Token)
        );

        // Assert
        Assert.AreEqual("Crawl has already started, a new run cannot be started with this instance.", exception.Message);

        cts.Cancel();
    }

    [TestMethod]
    public void CrawlInProgress_ReturnsFalse_WhenNoCrawlIsRunning()
    {
        // Arrange
        var mockHttpClientFactory = Substitute.For<IHttpClientFactory>();
        var mockHttpClient = new HttpClient();
        mockHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(mockHttpClient);

        var mockOptions = Options.Create(GetDefaultConfig());
        var mockLogger = Substitute.For<ILogger<CrawlerFetcher>>();

        // Act
        var service = new CrawlerService(mockHttpClientFactory, mockOptions, mockLogger);

        // Assert
        Assert.IsTrue(service.CanStartCrawling);
    }
}