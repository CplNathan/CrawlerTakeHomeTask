namespace CrawlerTakeHomeTask.Services.Crawler;

using AngleSharp.Html.Dom;
using CrawlerTakeHomeTask.Models;
using System.Collections.Generic;

public interface ICrawlerParser
{
    IAsyncEnumerable<PageModel> DiscoverUrls(IHtmlDocument document, Uri parentUrl, int parentDepth, Uri allowedDomain, CancellationToken token);
}
