namespace CrawlerTakeHomeTask.Services.Crawler;

using AngleSharp.Html.Dom;

public interface ICrawlerFetcher
{
    Task<IHtmlDocument?> GetPage(Uri pageUrl, CancellationToken token);
}
