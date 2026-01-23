using System.Linq;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Materializer;

/// <summary>
/// A materializer that uses snapshots to accelerate replay.
///
/// Checks for existing snapshots before replaying events. If a snapshot exists
/// at or before the target tick, it loads the snapshot and replays only the
/// tail of events after the snapshot.
/// </summary>
public sealed class SnapshottingPlateTopologyMaterializer
{
    private readonly ITopologyEventStore _eventStore;
    private readonly IPlateTopologySnapshotStore _snapshotStore;
    private readonly PlateTopologyMaterializer _inner;

    public SnapshottingPlateTopologyMaterializer(
        ITopologyEventStore eventStore,
        IPlateTopologySnapshotStore snapshotStore)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(snapshotStore);

        _eventStore = eventStore;
        _snapshotStore = snapshotStore;
        _inner = new PlateTopologyMaterializer(eventStore);
    }

    /// <summary>
    /// Materializes topology state at a specific tick, using snapshots if available.
    /// </summary>
    /// <param name="stream">The truth stream identity.</param>
    /// <param name="targetTick">The target simulation tick.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The materialization result with snapshot hit indicator.</returns>
    public async Task<MaterializationResult> MaterializeAtTickAsync(
        TruthStreamIdentity stream,
        CanonicalTick targetTick,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var key = new PlateTopologyMaterializationKey(stream, targetTick.Value);

        // Check for existing snapshot at target tick
        var snapshot = await _snapshotStore.GetSnapshotAsync(key, cancellationToken);
        if (snapshot.HasValue)
        {
            var stateFromSnapshot = FromSnapshot(snapshot.Value);
            return new MaterializationResult(key, stateFromSnapshot, true);
        }

        // No snapshot - do full replay with tick-based cutoff
        var state = await _inner.MaterializeAtTickAsync(stream, targetTick, cancellationToken);

        // Save snapshot for "latest" queries (when targetTick is at or beyond head)
        var head = await _eventStore.GetLastSequenceAsync(stream, cancellationToken);
        if (head.HasValue && state.LastEventSequence >= head.Value)
        {
            var toSave = ToSnapshot(new PlateTopologyMaterializationKey(stream, state.LastEventSequence), state);
            await _snapshotStore.SaveSnapshotAsync(toSave, cancellationToken);
        }

        return new MaterializationResult(key, state, false);
    }

    /// <summary>
    /// Materializes topology state at a specific sequence, using snapshots if available.
    /// </summary>
    /// <param name="stream">The truth stream identity.</param>
    /// <param name="targetSequence">The target sequence number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The materialization result with snapshot hit indicator.</returns>
    public async Task<MaterializationResult> MaterializeAtSequenceAsync(
        TruthStreamIdentity stream,
        long targetSequence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var key = new PlateTopologyMaterializationKey(stream, targetSequence);

        var snapshot = await _snapshotStore.GetSnapshotAsync(key, cancellationToken);
        if (snapshot.HasValue)
        {
            var stateFromSnapshot = FromSnapshot(snapshot.Value);
            return new MaterializationResult(key, stateFromSnapshot, true);
        }

        var state = await _inner.MaterializeAtSequenceAsync(stream, targetSequence, cancellationToken);

        // Save snapshot for "latest" queries
        var head = await _eventStore.GetLastSequenceAsync(stream, cancellationToken);
        if (head.HasValue && targetSequence >= head.Value)
        {
            var toSave = ToSnapshot(new PlateTopologyMaterializationKey(stream, head.Value), state);
            await _snapshotStore.SaveSnapshotAsync(toSave, cancellationToken);
        }

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

    private static PlateTopologyState FromSnapshot(PlateTopologySnapshot snapshot)
    {
        var state = new PlateTopologyState(snapshot.Key.Stream);

        foreach (var plate in snapshot.Plates)
        {
            state.Plates[plate.PlateId] = plate;
        }

        foreach (var boundary in snapshot.Boundaries)
        {
            state.Boundaries[boundary.BoundaryId] = boundary;
        }

        foreach (var junction in snapshot.Junctions)
        {
            state.Junctions[junction.JunctionId] = junction;
        }

        state.SetLastEventSequence(snapshot.LastEventSequence);

        state.RebuildIndices();
        return state;
    }

    private static PlateTopologySnapshot ToSnapshot(PlateTopologyMaterializationKey key, PlateTopologyState state)
    {
        var plates = state.Plates.Values.OrderBy(p => p.PlateId.Value, GuidOrdering.Rfc4122Comparer).ToArray();
        var boundaries = state.Boundaries.Values.OrderBy(b => b.BoundaryId.Value, GuidOrdering.Rfc4122Comparer).ToArray();
        var junctions = state.Junctions.Values.OrderBy(j => j.JunctionId.Value, GuidOrdering.Rfc4122Comparer).ToArray();

        return new PlateTopologySnapshot(
            key,
            state.LastEventSequence,
            plates,
            boundaries,
            junctions);
    }

    public readonly record struct MaterializationResult(
        PlateTopologyMaterializationKey Key,
        PlateTopologyState State,
        bool FromSnapshot);
}
