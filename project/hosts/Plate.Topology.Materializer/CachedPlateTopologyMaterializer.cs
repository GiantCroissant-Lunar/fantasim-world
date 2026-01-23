using System.Collections.Concurrent;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Materializer;

/// <summary>
/// A caching wrapper around PlateTopologyMaterializer.
///
/// Caches materialized states by (stream, tick) key to avoid redundant replays.
/// Note: Cache keys use tick values, not sequence numbers.
/// </summary>
public sealed class CachedPlateTopologyMaterializer
{
    private readonly PlateTopologyMaterializer _inner;
    private readonly ConcurrentDictionary<PlateTopologyMaterializationKey, PlateTopologyState> _cache = new();

    public CachedPlateTopologyMaterializer(ITopologyEventStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _inner = new PlateTopologyMaterializer(store);
    }

    /// <summary>
    /// Materializes topology state at a specific tick, using cache if available.
    /// </summary>
    /// <param name="stream">The truth stream identity.</param>
    /// <param name="targetTick">The target simulation tick.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The materialization result with cache hit indicator.</returns>
    public async Task<MaterializationResult> MaterializeAtTickAsync(
        TruthStreamIdentity stream,
        CanonicalTick targetTick,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var key = new PlateTopologyMaterializationKey(stream, targetTick.Value);
        if (_cache.TryGetValue(key, out var cached))
        {
            return new MaterializationResult(key, cached, true);
        }

        var state = await _inner.MaterializeAtTickAsync(stream, targetTick, cancellationToken);
        _cache.TryAdd(key, state);
        return new MaterializationResult(key, state, false);
    }

    /// <summary>
    /// Materializes topology state at a specific sequence, using cache if available.
    /// </summary>
    /// <param name="stream">The truth stream identity.</param>
    /// <param name="targetSequence">The target sequence number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The materialization result with cache hit indicator.</returns>
    public async Task<MaterializationResult> MaterializeAtSequenceAsync(
        TruthStreamIdentity stream,
        long targetSequence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Use sequence as the tick component for cache key (sequence-based queries)
        var key = new PlateTopologyMaterializationKey(stream, targetSequence);
        if (_cache.TryGetValue(key, out var cached))
        {
            return new MaterializationResult(key, cached, true);
        }

        var state = await _inner.MaterializeAtSequenceAsync(stream, targetSequence, cancellationToken);
        _cache.TryAdd(key, state);
        return new MaterializationResult(key, state, false);
    }

    /// <summary>
    /// [OBSOLETE] Use MaterializeAtTickAsync(stream, CanonicalTick) or MaterializeAtSequenceAsync instead.
    /// </summary>
    [Obsolete("Use MaterializeAtTickAsync(stream, CanonicalTick) or MaterializeAtSequenceAsync instead.")]
    public Task<MaterializationResult> MaterializeAtTickAsync(
        TruthStreamIdentity stream,
        long tick,
        CancellationToken cancellationToken = default)
    {
        // Old behavior was sequence-based
        return MaterializeAtSequenceAsync(stream, tick, cancellationToken);
    }

    public void Clear() => _cache.Clear();

    public readonly record struct MaterializationResult(
        PlateTopologyMaterializationKey Key,
        PlateTopologyState State,
        bool FromCache);
}
