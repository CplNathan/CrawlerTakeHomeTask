namespace CrawlerTakeHomeTask.Services.Channels;

using CrawlerTakeHomeTask.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Channels;

internal sealed class DistributedChannel(IChannel channel) : ICrawlerChannel
{
    private readonly Channel<PageModel> items = Channel.CreateUnbounded<PageModel>();
    private bool requestingClosure = false;
    private string consumerName = string.Empty;

    public ChannelReader<PageModel> Items => items;

    public async Task Hookup(CancellationToken token)
    {
        await channel.QueueDeclareAsync(queue: "task_queue", durable: false, exclusive: false, autoDelete: true, arguments: null, cancellationToken: token);
        await channel.BasicQosAsync(0, 10, false, token);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                var pageModel = new PageModel(new Uri(message));
                await this.WriteInternal(pageModel, token, true);

                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: token);
            }
            catch
            {
                await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        consumerName = await channel.BasicConsumeAsync("task_queue", autoAck: false, consumer: consumer, cancellationToken: token);
    }

    public ValueTask Write(PageModel item, CancellationToken token)
        => this.WriteInternal(item, token);

    public async ValueTask<int> Length()
    {
        try
        {
            var queueInfo = await channel.QueueDeclarePassiveAsync("task_queue");

            return (int)queueInfo.MessageCount;
        }
        catch
        {
            return 0;
        }
    }

    public async ValueTask<bool> TryComplete()
    {
        if (Items.Count > 0) return false;
        if (await Length() > 0) return false;

        requestingClosure = true;
        items.Writer.TryComplete();

        await channel.BasicCancelAsync(consumerName);

        return true;
    }

    private async ValueTask WriteInternal(PageModel item, CancellationToken token, bool incoming = false)
    {
        if (requestingClosure || incoming)
        {
            if (!items.Writer.TryWrite(item))
            {
                await items.Writer.WriteAsync(item, token);
            }
        }
        else
        {
            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "task_queue", mandatory: true, body: Encoding.UTF8.GetBytes(item.Url.ToString()), cancellationToken: token);
        }
    }
}
