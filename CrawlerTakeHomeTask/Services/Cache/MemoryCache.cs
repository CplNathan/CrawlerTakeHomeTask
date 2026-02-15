namespace CrawlerTakeHomeTask.Services.Cache;

using System.Collections.Concurrent;

internal class MemoryCache : ICrawlerCache
{
    private readonly ConcurrentDictionary<string, object?> storageBacking = new();

    private long activeWorkCount = 0;

    public ValueTask<bool> SetAdd(string key)
        => ValueTask.FromResult(storageBacking.TryAdd(key, null));

    public ValueTask<int> StoredItems()
        => ValueTask.FromResult(storageBacking.Count);

    public ValueTask<long> IncrementWorkers()
        => ValueTask.FromResult(Interlocked.Increment(ref activeWorkCount));

    public ValueTask<long> DecrementWorkers()
        => ValueTask.FromResult(Interlocked.Decrement(ref activeWorkCount));

    public ValueTask<long> Workers()
        => ValueTask.FromResult(Interlocked.Read(ref activeWorkCount));
}
