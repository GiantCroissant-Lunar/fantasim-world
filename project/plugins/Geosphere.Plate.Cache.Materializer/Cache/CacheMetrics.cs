using FantaSim.Geosphere.Plate.Cache.Contracts;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Cache;

internal sealed class CacheMetrics : ICacheMetrics
{
    private long _hitCount;
    private long _missCount;
    private long _invalidationCount;
    private long _evictionCount;
    private long _totalComputeTimeMs;
    private long _computeCount;

    public long HitCount => Interlocked.Read(ref _hitCount);

    public long MissCount => Interlocked.Read(ref _missCount);

    public long InvalidationCount => Interlocked.Read(ref _invalidationCount);

    public long EvictionCount => Interlocked.Read(ref _evictionCount);

    public double HitRatio
    {
        get
        {
            var hits = HitCount;
            var total = hits + MissCount;
            return total == 0 ? 0.0 : (double)hits / total;
        }
    }

    public TimeSpan AverageComputeTime
    {
        get
        {
            var count = Interlocked.Read(ref _computeCount);
            if (count == 0) return TimeSpan.Zero;
            var totalMs = Interlocked.Read(ref _totalComputeTimeMs);
            return TimeSpan.FromMilliseconds((double)totalMs / count);
        }
    }

    public void RecordHit() => Interlocked.Increment(ref _hitCount);

    public void RecordMiss() => Interlocked.Increment(ref _missCount);

    public void RecordInvalidation(long count)
    {
        if (count <= 0) return;
        Interlocked.Add(ref _invalidationCount, count);
    }

    public void RecordEviction(long count)
    {
        if (count <= 0) return;
        Interlocked.Add(ref _evictionCount, count);
    }

    public void RecordComputeTime(long elapsedMs)
    {
        Interlocked.Add(ref _totalComputeTimeMs, elapsedMs);
        Interlocked.Increment(ref _computeCount);
    }
}
