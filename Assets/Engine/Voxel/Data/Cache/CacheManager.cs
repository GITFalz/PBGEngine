using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using PBG.MathLibrary;

public static class CacheManager
{
    private static ConcurrentDictionary<Vector2i, ConcurrentDictionary<int, ColumnCache>> Caches = [];

    public static readonly Queue<ColumnCache> GenerationQueue = new();
    private static readonly object _queueLock = new();

    public static ColumnCache GetOrAdd(Vector3i key)
    {
        var chunkKey = new Vector2i(key.X, key.Y);
        int cacheId = key.Z;

        var chunkCaches = Caches.GetOrAdd(
            chunkKey,
            _ => new ConcurrentDictionary<int, ColumnCache>()
        );

        return chunkCaches.GetOrAdd(cacheId, _ => new ColumnCache(key));
    }

    public static ColumnCache GetCacheBlocking(Vector3i worldPosition, Vector3i key, bool urgent = false)
    {
        var cache = GetOrAdd(key);
        cache.WorldPosition = worldPosition;

        // If already ready, return instantly
        if (cache.IsReady)
            return cache;

        // Mark priority if needed
        if (urgent)
            cache.Priority = 0;

        // If not already queued for generation, queue it
        if (Interlocked.CompareExchange(ref cache.Initializing, 1, 0) == 0)
        {
            lock (_queueLock)
            {
                GenerationQueue.Enqueue(cache);
            }
        }

        // Wait until main thread generates it
        //cache.WaitHandle.Wait();

        return cache;
    }


    public static void Remove(Vector3i key)
    {
        var chunkKey = new Vector2i(key.X, key.Y);
        int cacheId = key.Z;

        if (!Caches.TryGetValue(chunkKey, out var chunkCaches))
            return;

        if (chunkCaches.TryRemove(cacheId, out var c))
        {
            c.WaitHandle.Dispose();
        }

        if (chunkCaches.IsEmpty)
            Caches.TryRemove(chunkKey, out _);
    }

    public static void RemoveChunk(Vector2i chunkKey)
    {
        if (!Caches.TryRemove(chunkKey, out var _))
            return;
    }

    public static void Clear()
    {
        Caches = [];
    }
}