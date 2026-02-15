namespace CrawlerTakeHomeTask.Services.Crawler;

using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

public sealed class CrawlerFetcher(IHttpClientFactory factory, ILogger<CrawlerFetcher> logger) : ICrawlerFetcher
{
    private readonly HtmlParser HtmlParser = new();
    private readonly HttpClient httpClient = factory.CreateClient(Program.CrawlerClientName);

    public async Task<IHtmlDocument?> GetPage(Uri pageUrl, CancellationToken token)
    {
        try
        {
            using var pageStream = await httpClient.GetStreamAsync(pageUrl, token).ConfigureAwait(false);

            var document = await HtmlParser.ParseDocumentAsync(pageStream, token).ConfigureAwait(false);
            document.Location.Href = pageUrl.AbsoluteUri;

            return document;
        }
        catch (TaskCanceledException canceledException) when (canceledException.InnerException is TimeoutException)
        {
            logger.LogWarning("Timed out visiting {page}", pageUrl);
        }
        catch (HttpRequestException requestException)
        {
            logger.LogError("Failed visiting {page} with error: {errorMessage}", pageUrl, requestException.Message);
        }

        return null;
    }
}
