namespace CrawlerTakeHomeTask.Services.Cache;

using StackExchange.Redis;

internal class DistributedCache(IDatabase database) : ICrawlerCache
{
    public async ValueTask<bool> SetAdd(string key)
        => await database.SetAddAsync(key, key);

    public ValueTask<int> StoredItems()
        => ValueTask.FromResult(0);

    public async ValueTask<long> IncrementWorkers()
        => await database.StringIncrementAsync("activeWorkCount", 1);

    public async ValueTask<long> DecrementWorkers()
        => await database.StringDecrementAsync("activeWorkCount", 1);

    public async ValueTask<long> Workers()
        => (int)await database.StringGetAsync("activeWorkCount");
}
