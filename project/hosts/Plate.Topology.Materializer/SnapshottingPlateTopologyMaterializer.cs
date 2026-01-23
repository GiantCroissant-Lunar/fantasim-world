using System.Linq;
using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Materializer;

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

    public async Task<MaterializationResult> MaterializeAtTickAsync(
        TruthStreamIdentity stream,
        long tick,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var key = new PlateTopologyMaterializationKey(stream, tick);

        var head = await _eventStore.GetLastSequenceAsync(stream, cancellationToken);
        var snapshotTick = head.HasValue && tick >= head.Value ? head.Value : tick;
        var snapshotKey = new PlateTopologyMaterializationKey(stream, snapshotTick);

        var snapshot = await _snapshotStore.GetSnapshotAsync(snapshotKey, cancellationToken);
        if (snapshot.HasValue)
        {
            var stateFromSnapshot = FromSnapshot(snapshot.Value);
            return new MaterializationResult(key, stateFromSnapshot, true);
        }

        var state = await _inner.MaterializeAtTickAsync(stream, tick, cancellationToken);

        if (head.HasValue && tick >= head.Value)
        {
            var toSave = ToSnapshot(new PlateTopologyMaterializationKey(stream, head.Value), state);
            await _snapshotStore.SaveSnapshotAsync(toSave, cancellationToken);
        }

        return new MaterializationResult(key, state, false);
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
