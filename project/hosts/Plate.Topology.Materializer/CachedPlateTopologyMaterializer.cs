using System.Collections.Concurrent;
using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Materializer;

public sealed class CachedPlateTopologyMaterializer
{
    private readonly PlateTopologyMaterializer _inner;
    private readonly ConcurrentDictionary<PlateTopologyMaterializationKey, PlateTopologyState> _cache = new();

    public CachedPlateTopologyMaterializer(ITopologyEventStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _inner = new PlateTopologyMaterializer(store);
    }

    public async Task<MaterializationResult> MaterializeAtTickAsync(
        TruthStreamIdentity stream,
        long tick,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var key = new PlateTopologyMaterializationKey(stream, tick);
        if (_cache.TryGetValue(key, out var cached))
        {
            return new MaterializationResult(key, cached, true);
        }

        var state = await _inner.MaterializeAtTickAsync(stream, tick, cancellationToken);
        _cache.TryAdd(key, state);
        return new MaterializationResult(key, state, false);
    }

    public void Clear() => _cache.Clear();

    public readonly record struct MaterializationResult(
        PlateTopologyMaterializationKey Key,
        PlateTopologyState State,
        bool FromCache);
}
