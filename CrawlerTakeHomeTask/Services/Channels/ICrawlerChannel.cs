namespace CrawlerTakeHomeTask.Services.Channels;

using CrawlerTakeHomeTask.Models;
using System.Threading.Channels;

public interface ICrawlerChannel
{
    ChannelReader<PageModel> Items { get; }

    Task Hookup(CancellationToken token);

    ValueTask Write(PageModel model, CancellationToken token);

    ValueTask<int> Length();

    ValueTask<bool> TryComplete();
}
