namespace CrawlerTakeHomeTask.Services.Cache;

public interface ICrawlerCache
{
    ValueTask<bool> SetAdd(string key);

    ValueTask<int> StoredItems();

    ValueTask<long> IncrementWorkers();

    ValueTask<long> DecrementWorkers();

    ValueTask<long> Workers();
}
