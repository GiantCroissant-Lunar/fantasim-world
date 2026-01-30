using System.Collections.Concurrent;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Capabilities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Topology.Materializer;

/// <summary>
/// A caching wrapper around PlateTopologyMaterializer.
///
/// Caches materialized states by (stream, tick, lastSequence) key to avoid redundant replays.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cache key includes last sequence number</b> to handle back-in-time events correctly.
/// If an event is later appended with tick <= targetTick, the cache entry becomes invalid
/// because the sequence number will have increased. This ensures correctness while still
/// providing cache hits when the event stream is unchanged.
/// </para>
/// </remarks>
public sealed class CachedPlateTopologyMaterializer
{
    private readonly ITopologyEventStore _store;
    private readonly PlateTopologyMaterializer _inner;
    private readonly ConcurrentDictionary<PlateTopologyMaterializationKey, PlateTopologyState> _cache = new();

    public CachedPlateTopologyMaterializer(ITopologyEventStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _inner = new PlateTopologyMaterializer(store);
    }

    /// <summary>
    /// Materializes topology state at a specific tick, using cache if available.
    /// </summary>
    /// <param name="stream">The truth stream identity.</param>
    /// <param name="targetTick">The target simulation tick.</param>
    /// <param name="mode">Tick materialization mode (default: Auto).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The materialization result with cache hit indicator.</returns>
    public async Task<MaterializationResult> MaterializeAtTickAsync(
        TruthStreamIdentity stream,
        CanonicalTick targetTick,
        TickMaterializationMode mode = TickMaterializationMode.Auto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Query last sequence to include in cache key (correctness for back-in-time events)
        var lastSeq = await GetLastSequenceAsync(stream, cancellationToken).ConfigureAwait(false);
        var key = new PlateTopologyMaterializationKey(stream, targetTick.Value, lastSeq);

        if (_cache.TryGetValue(key, out var cached))
        {
            return new MaterializationResult(key, cached, true);
        }

        var state = await _inner.MaterializeAtTickAsync(stream, targetTick, mode, cancellationToken).ConfigureAwait(false);
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

        // Query last sequence to include in cache key
        var lastSeq = await GetLastSequenceAsync(stream, cancellationToken).ConfigureAwait(false);

        // Use sequence as the tick component for cache key (sequence-based queries)
        var key = new PlateTopologyMaterializationKey(stream, targetSequence, lastSeq);
        if (_cache.TryGetValue(key, out var cached))
        {
            return new MaterializationResult(key, cached, true);
        }

        var state = await _inner.MaterializeAtSequenceAsync(stream, targetSequence, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Gets the last sequence number for a stream, or -1 if stream is empty.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="ITopologyEventStore.GetLastSequenceAsync"/> when available,
    /// returning -1 for empty/non-existent streams to ensure consistent cache keys.
    /// </remarks>
    private async Task<long> GetLastSequenceAsync(
        TruthStreamIdentity stream,
        CancellationToken cancellationToken)
    {
        var lastSeq = await _store.GetLastSequenceAsync(stream, cancellationToken).ConfigureAwait(false);
        return lastSeq ?? -1L;
    }

    public readonly record struct MaterializationResult(
        PlateTopologyMaterializationKey Key,
        PlateTopologyState State,
        bool FromCache);
}
