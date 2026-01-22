using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;
using BoundaryEntity = Plate.Topology.Contracts.Entities.Boundary;
using JunctionEntity = Plate.Topology.Contracts.Entities.Junction;
using PlateEntity = Plate.Topology.Contracts.Entities.Plate;

namespace Plate.Topology.Materializer;

/// <summary>
/// Materializes PlateTopologyState by replaying events from ITopologyEventStore per FR-007.
///
/// The materializer reads events from the event store for a given truth stream and
/// applies them in order to build the current topology state. This enables deterministic
/// replay where the same event stream always produces identical state.
///
/// Per FR-007, materialization is event-only: no external data sources or solver
/// execution is required. All information needed to reconstruct state is contained
/// in the event stream.
/// </summary>
public sealed class PlateTopologyMaterializer
{
    private readonly ITopologyEventStore _store;

    /// <summary>
    /// Initializes a new instance of PlateTopologyMaterializer.
    /// </summary>
    /// <param name="store">The event store to read events from.</param>
    public PlateTopologyMaterializer(ITopologyEventStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <summary>
    /// Materializes the current topology state by replaying all events from the stream.
    ///
    /// Reads all events from the event store starting from sequence 0 and applies
    /// them in order to build the materialized state. This produces deterministic
    /// results: the same event stream always produces identical state (per SC-001).
    ///
    /// For empty streams, returns an empty state with LastEventSequence = -1 (per SC-008).
    /// </summary>
    /// <param name="stream">The truth stream identity to materialize.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The materialized topology state containing all plates, boundaries, and junctions.
    /// </returns>
    public async Task<PlateTopologyState> MaterializeAsync(
        TruthStreamIdentity stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var state = new PlateTopologyState(stream);

        await foreach (var evt in _store.ReadAsync(stream, 0, cancellationToken))
        {
            ApplyEvent(state, evt);
        }

        return state;
    }

    public async Task<PlateTopologyState> MaterializeAtTickAsync(
        TruthStreamIdentity stream,
        long tick,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (tick < -1)
            throw new ArgumentOutOfRangeException(nameof(tick), "Tick must be >= -1");

        var state = new PlateTopologyState(stream);
        if (tick < 0)
            return state;

        await foreach (var evt in _store.ReadAsync(stream, 0, cancellationToken))
        {
            if (evt.Sequence > tick)
                break;

            ApplyEvent(state, evt);
        }

        return state;
    }

    /// <summary>
    /// Materializes topology state from a specific sequence onwards.
    ///
    /// Useful for incremental updates or replaying from a checkpoint. Reads events
    /// starting from the specified sequence and applies them to build state.
    /// </summary>
    /// <param name="stream">The truth stream identity to materialize.</param>
    /// <param name="fromSequence">The starting sequence number (inclusive).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The materialized topology state from the specified sequence onwards.
    /// </returns>
    public async Task<PlateTopologyState> MaterializeFromAsync(
        TruthStreamIdentity stream,
        long fromSequence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (fromSequence < 0)
            throw new ArgumentOutOfRangeException(nameof(fromSequence), "Sequence must be non-negative");

        var state = new PlateTopologyState(stream);

        await foreach (var evt in _store.ReadAsync(stream, fromSequence, cancellationToken))
        {
            ApplyEvent(state, evt);
        }

        return state;
    }

    /// <summary>
    /// Applies a single event to the materialized state.
    ///
    /// First validates the event against invariants, then applies it if valid.
    /// For invalid events, throws InvalidOperationException with clear error message.
    /// </summary>
    /// <param name="state">The state to update.</param>
    /// <param name="evt">The event to apply.</param>
    private static void ApplyEvent(PlateTopologyState state, IPlateTopologyEvent evt)
    {
        // Validate event against invariants (throws on violation)
        InvariantValidator.ValidateEvent(state, evt);

        // Apply the event to state
        switch (evt)
        {
            case PlateCreatedEvent plateCreated:
                ApplyPlateCreated(state, plateCreated);
                break;

            case PlateRetiredEvent plateRetired:
                ApplyPlateRetired(state, plateRetired);
                break;

            case BoundaryCreatedEvent boundaryCreated:
                ApplyBoundaryCreated(state, boundaryCreated);
                break;

            case BoundaryTypeChangedEvent boundaryTypeChanged:
                ApplyBoundaryTypeChanged(state, boundaryTypeChanged);
                break;

            case BoundaryGeometryUpdatedEvent boundaryGeometryUpdated:
                ApplyBoundaryGeometryUpdated(state, boundaryGeometryUpdated);
                break;

            case BoundaryRetiredEvent boundaryRetired:
                ApplyBoundaryRetired(state, boundaryRetired);
                break;

            case JunctionCreatedEvent junctionCreated:
                ApplyJunctionCreated(state, junctionCreated);
                break;

            case JunctionUpdatedEvent junctionUpdated:
                ApplyJunctionUpdated(state, junctionUpdated);
                break;

            case JunctionRetiredEvent junctionRetired:
                ApplyJunctionRetired(state, junctionRetired);
                break;

            default:
                state.Violations.Add(new InvariantViolation(
                    "UnknownEvent",
                    $"Unknown event type: {evt.EventType}",
                    evt.Sequence
                ));
                break;
        }

        state.SetLastEventSequence(evt.Sequence);
    }

    private static void ApplyPlateCreated(PlateTopologyState state, PlateCreatedEvent evt)
    {
        state.Plates[evt.PlateId] = new PlateEntity(evt.PlateId, false, null);
    }

    private static void ApplyPlateRetired(PlateTopologyState state, PlateRetiredEvent evt)
    {
        var plate = state.Plates[evt.PlateId];
        state.Plates[evt.PlateId] = plate with { IsRetired = true, RetirementReason = evt.Reason };
    }

    private static void ApplyBoundaryCreated(PlateTopologyState state, BoundaryCreatedEvent evt)
    {
        state.Boundaries[evt.BoundaryId] = new BoundaryEntity(
            evt.BoundaryId,
            evt.PlateIdLeft,
            evt.PlateIdRight,
            evt.BoundaryType,
            evt.Geometry,
            false,
            null
        );
    }

    private static void ApplyBoundaryTypeChanged(PlateTopologyState state, BoundaryTypeChangedEvent evt)
    {
        var boundary = state.Boundaries[evt.BoundaryId];
        state.Boundaries[evt.BoundaryId] = boundary with { BoundaryType = evt.NewType };
    }

    private static void ApplyBoundaryGeometryUpdated(PlateTopologyState state, BoundaryGeometryUpdatedEvent evt)
    {
        var boundary = state.Boundaries[evt.BoundaryId];
        state.Boundaries[evt.BoundaryId] = boundary with { Geometry = evt.NewGeometry };
    }

    private static void ApplyBoundaryRetired(PlateTopologyState state, BoundaryRetiredEvent evt)
    {
        var boundary = state.Boundaries[evt.BoundaryId];
        state.Boundaries[evt.BoundaryId] = boundary with { IsRetired = true, RetirementReason = evt.Reason };
    }

    private static void ApplyJunctionCreated(PlateTopologyState state, JunctionCreatedEvent evt)
    {
        state.Junctions[evt.JunctionId] = new JunctionEntity(
            evt.JunctionId,
            evt.BoundaryIds,
            evt.Location,
            false,
            null
        );
    }

    private static void ApplyJunctionUpdated(PlateTopologyState state, JunctionUpdatedEvent evt)
    {
        var junction = state.Junctions[evt.JunctionId];
        var updatedJunction = junction;

        if (evt.NewBoundaryIds is not null)
        {
            updatedJunction = updatedJunction with { BoundaryIds = evt.NewBoundaryIds };
        }

        if (evt.NewLocation is not null)
        {
            updatedJunction = updatedJunction with { Location = evt.NewLocation.Value };
        }

        state.Junctions[evt.JunctionId] = updatedJunction;
    }

    private static void ApplyJunctionRetired(PlateTopologyState state, JunctionRetiredEvent evt)
    {
        var junction = state.Junctions[evt.JunctionId];
        state.Junctions[evt.JunctionId] = junction with { IsRetired = true, RetirementReason = evt.Reason };
    }
}
