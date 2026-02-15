namespace CrawlerTakeHomeTask.Services.Channels;

using CrawlerTakeHomeTask.Models;
using System.Threading.Channels;

internal sealed class LocalChannel(Channel<PageModel> channel) : ICrawlerChannel
{
    public ChannelReader<PageModel> Items => channel.Reader;

    public Task Hookup(CancellationToken token) => Task.CompletedTask;

    public async ValueTask Write(PageModel model, CancellationToken token)
    {
        if (!channel.Writer.TryWrite(model))
        {
            await channel.Writer.WriteAsync(model, token);
        }
    }

    public ValueTask<int> Length()
        => ValueTask.FromResult(Items.Count);

    public async ValueTask<bool> TryComplete()
    {
        if (await Length() > 0) return false;

        channel.Writer.TryComplete();

        return true;
    }
}
