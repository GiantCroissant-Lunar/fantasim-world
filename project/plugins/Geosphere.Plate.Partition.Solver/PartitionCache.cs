using System.Collections.Concurrent;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using Microsoft.Extensions.Logging;

namespace FantaSim.Geosphere.Plate.Partition.Solver;

/// <summary>
/// Thread-safe cache for plate partition results.
/// Keys are computed from StreamIdentity (topology hash + version + policy).
/// RFC-V2-0047 §4.2.
/// </summary>
public sealed class PartitionCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly TimeSpan _cacheDuration;
    private readonly ILogger<PartitionCache>? _logger;
    private long _hitCount;
    private long _missCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartitionCache"/>.
    /// </summary>
    /// <param name="cacheDuration">Duration before cache entries expire. Default is 5 minutes.</param>
    /// <param name="logger">Optional logger for cache operations.</param>
    public PartitionCache(TimeSpan? cacheDuration = null, ILogger<PartitionCache>? logger = null)
    {
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _cacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5);
        _logger = logger;
    }

    /// <summary>
    /// Gets the current number of entries in the cache.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Gets the cache hit count.
    /// </summary>
    public long HitCount => Interlocked.Read(ref _hitCount);

    /// <summary>
    /// Gets the cache miss count.
    /// </summary>
    public long MissCount => Interlocked.Read(ref _missCount);

    /// <summary>
    /// Gets the cache hit ratio (0.0 to 1.0).
    /// </summary>
    public double HitRatio
    {
        get
        {
            var hits = Interlocked.Read(ref _hitCount);
            var misses = Interlocked.Read(ref _missCount);
            var total = hits + misses;
            return total > 0 ? (double)hits / total : 0.0;
        }
    }

    /// <summary>
    /// Attempts to get a cached partition result.
    /// </summary>
    /// <param name="streamIdentity">The computed stream identity.</param>
    /// <param name="result">The cached result if found and not expired.</param>
    /// <returns>True if a valid cached entry was found.</returns>
    public bool TryGet(StreamIdentity streamIdentity, out PlatePartitionResult result)
    {
        var key = streamIdentity.CombinedHash;

        if (_cache.TryGetValue(key, out var entry))
        {
            if (!entry.IsExpired && entry.StreamIdentity == streamIdentity)
            {
                Interlocked.Increment(ref _hitCount);
                _logger?.LogDebug("Cache hit for stream {StreamHash}", streamIdentity.TopologyStreamHash);
                result = entry.Result;
                return true;
            }

            // Expired or identity mismatch - remove it
            _cache.TryRemove(key, out _);
        }

        Interlocked.Increment(ref _missCount);
        _logger?.LogDebug("Cache miss for stream {StreamHash}", streamIdentity.TopologyStreamHash);
        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to get a cached partition result by topology stream and tolerance policy.
    /// </summary>
    public bool TryGet(
        TruthStreamIdentity topologyStream,
        TolerancePolicy tolerancePolicy,
        StreamIdentityComputer identityComputer,
        out PlatePartitionResult result)
    {
        var streamIdentity = identityComputer.ComputeStreamIdentity(
            topologyStream,
            0, // Version not used for cache lookup
            tolerancePolicy);

        return TryGet(streamIdentity, out result);
    }

    /// <summary>
    /// Stores a partition result in the cache.
    /// </summary>
    /// <param name="streamIdentity">The computed stream identity.</param>
    /// <param name="result">The partition result to cache.</param>
    public void Set(StreamIdentity streamIdentity, PlatePartitionResult result)
    {
        var key = streamIdentity.CombinedHash;
        var entry = new CacheEntry(streamIdentity, result, _cacheDuration);

        _cache[key] = entry;
        _logger?.LogDebug("Cached partition result for stream {StreamHash}", streamIdentity.TopologyStreamHash);
    }

    /// <summary>
    /// Invalidates all cache entries matching a topology stream hash prefix.
    /// </summary>
    public void InvalidateByTopology(string topologyStreamHashPrefix)
    {
        var keysToRemove = _cache
            .Where(kvp => kvp.Value.StreamIdentity.TopologyStreamHash.StartsWith(topologyStreamHashPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }

        _logger?.LogInformation("Invalidated {Count} cache entries for topology prefix {Prefix}",
            keysToRemove.Count, topologyStreamHashPrefix);
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        Interlocked.Exchange(ref _hitCount, 0);
        Interlocked.Exchange(ref _missCount, 0);
        _logger?.LogInformation("Cache cleared");
    }

    /// <summary>
    /// Removes expired entries from the cache.
    /// </summary>
    public void EvictExpired()
    {
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger?.LogDebug("Evicted {Count} expired cache entries", expiredKeys.Count);
        }
    }

    /// <summary>
    /// Internal cache entry structure.
    /// </summary>
    private readonly struct CacheEntry
    {
        public StreamIdentity StreamIdentity { get; }
        public PlatePartitionResult Result { get; }
        public DateTimeOffset CreatedAt { get; }
        public TimeSpan Duration { get; }

        public bool IsExpired => DateTimeOffset.UtcNow > CreatedAt + Duration;

        public CacheEntry(StreamIdentity streamIdentity, PlatePartitionResult result, TimeSpan duration)
        {
            StreamIdentity = streamIdentity;
            Result = result;
            CreatedAt = DateTimeOffset.UtcNow;
            Duration = duration;
        }
    }
}

/// <summary>
/// Options for configuring the partition cache behavior.
/// </summary>
public sealed class PartitionCacheOptions
{
    /// <summary>
    /// Default cache duration.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum number of entries before aggressive eviction.
    /// </summary>
    public int MaxEntries { get; set; } = 1000;

    /// <summary>
    /// Whether to enable automatic expiration checking.
    /// </summary>
    public bool EnableAutoEviction { get; set; } = true;

    /// <summary>
    /// Interval for automatic eviction scans.
    /// </summary>
    public TimeSpan EvictionInterval { get; set; } = TimeSpan.FromMinutes(1);
}
