namespace CrawlerTakeHomeTask.Models;

using System;

public class PageModel
{
    public override int GetHashCode()
        => this.RelativePath.GetHashCode();

    public PageModel(Uri pageUrl)
    {
        ArgumentNullException.ThrowIfNull(pageUrl);

        this.Url = pageUrl;
    }

    public PageModel(Uri parentUrl, int parentDepth, Uri childUrl)
    {
        this.Url = new(parentUrl, childUrl);
        this.Depth = parentDepth + 1;
    }

    public bool IsRootPage => Depth == 0;

    public string RelativePath => Url.IsAbsoluteUri ? Url.AbsolutePath : Url.OriginalString;

    public Uri Url { get; private init; }

    public int Depth { get; private init; }
}
