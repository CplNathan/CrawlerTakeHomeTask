namespace CrawlerTakeHomeTask.Services.Crawler;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using CrawlerTakeHomeTask.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal class CrawlerParser : ICrawlerParser
{
    private static readonly string[] AllowedSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps];

    public async IAsyncEnumerable<PageModel> DiscoverUrls(IHtmlDocument document, Uri parentUrl, int parentDepth, Uri allowedDomain, [EnumeratorCancellation] CancellationToken token)
    {
        var navLinks = document.Links.ToAsyncEnumerable()
            .OfType<IHtmlAnchorElement>()
            .Select(link => link.Href)
            .Select(link => new Uri(link, UriKind.Absolute))
            .Where(uri => FilterPage(uri, allowedDomain));
        await foreach (var (index, uri) in navLinks.Index().WithCancellation(token))
        {
            if (token.IsCancellationRequested) yield break;

            yield return new PageModel(parentUrl, parentDepth, uri);
        }
    }

    private static bool FilterPage(Uri page, Uri allowedDomain)
    {
        static bool IsValidPage(Uri page)
            => !page.IsAbsoluteUri || (page.IsAbsoluteUri && AllowedSchemes.Contains(page.Scheme));

        static bool IsInternalPage(Uri page, Uri allowedDomain)
            => !page.IsAbsoluteUri || page.IsAbsoluteUri && page.Host.Equals(allowedDomain.Host, StringComparison.InvariantCultureIgnoreCase);

        return IsInternalPage(page, allowedDomain) && IsValidPage(page);
    }
}
