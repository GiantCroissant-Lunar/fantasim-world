using FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Kinematics.Materializer;

public sealed class PlateKinematicsMaterializer
{
    private readonly IKinematicsEventStore _store;

    public PlateKinematicsMaterializer(IKinematicsEventStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public async Task<PlateKinematicsState> MaterializeAsync(
        TruthStreamIdentity stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var state = new PlateKinematicsState(stream);

        await foreach (var evt in _store.ReadAsync(stream, 0, cancellationToken).ConfigureAwait(false))
        {
            ApplyEvent(state, evt);
        }

        state.RebuildIndices();
        return state;
    }

    private static void ApplyEvent(PlateKinematicsState state, IPlateKinematicsEvent evt)
    {
        switch (evt)
        {
            case MotionSegmentUpsertedEvent upsert:
                state.UpsertSegment(upsert.PlateId, upsert.SegmentId, upsert.TickA, upsert.TickB, upsert.StageRotation);
                break;

            case MotionSegmentRetiredEvent retired:
                state.RetireSegment(retired.PlateId, retired.SegmentId);
                break;

            case PlateMotionModelAssignedEvent:
                // Optional metadata event: does not affect rotation computation in v0.
                break;
        }

        state.SetLastEventSequence(evt.Sequence);
    }
}
