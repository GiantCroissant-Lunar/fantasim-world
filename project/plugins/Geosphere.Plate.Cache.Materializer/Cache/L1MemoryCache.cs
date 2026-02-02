using System.Collections.Concurrent;
using FantaSim.Geosphere.Plate.Cache.Contracts;

namespace FantaSim.Geosphere.Plate.Cache.Materializer.Cache;

internal sealed class L1MemoryCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    public int Count => _cache.Count;

    public bool TryGet<T>(string key, out T? value, out DerivedProductProvenance? provenance)
    {
        if (_cache.TryGetValue(key, out var entry) && entry.Value is T typedValue)
        {
            value = typedValue;
            provenance = entry.Provenance;
            return true;
        }

        value = default;
        provenance = null;
        return false;
    }

    public void Set<T>(string key, T value, DerivedProductProvenance provenance)
    {
        _cache[key] = new CacheEntry(value!, provenance);
    }

    public int Invalidate(Func<string, bool> predicate)
    {
        var keysToRemove = _cache.Keys.Where(predicate).ToList();
        var count = 0;
        foreach (var key in keysToRemove)
        {
            if (_cache.TryRemove(key, out _))
            {
                count++;
            }
        }
        return count;
    }

    public bool Invalidate(string key) => _cache.TryRemove(key, out _);

    public int Clear()
    {
        var count = _cache.Count;
        _cache.Clear();
        return count;
    }

    private readonly record struct CacheEntry(object Value, DerivedProductProvenance Provenance);
}
