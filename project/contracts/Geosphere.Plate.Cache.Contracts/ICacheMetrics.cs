namespace FantaSim.Geosphere.Plate.Cache.Contracts;

public interface ICacheMetrics
{
    long HitCount { get; }

    long MissCount { get; }

    long InvalidationCount { get; }

    long EvictionCount { get; }

    double HitRatio { get; }

    TimeSpan AverageComputeTime { get; }
}
