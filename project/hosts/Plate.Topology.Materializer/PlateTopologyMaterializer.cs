using System.Diagnostics;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Capabilities;
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
///
/// Two cutoff modes are supported:
/// - Sequence-based: applies events up to a target sequence number (for deterministic replay)
/// - Tick-based: applies events where event.Tick &lt;= targetTick (for simulation time queries)
///
/// Tick-based materialization supports three modes via <see cref="TickMaterializationMode"/>:
/// - ScanAll: always correct, scans all events
/// - BreakOnFirstBeyondTick: fast but only safe for tick-monotone streams
/// - Auto: uses stream capabilities to choose the best mode
/// </summary>
public sealed class PlateTopologyMaterializer
{
    private readonly ITopologyEventStore _store;
    private readonly ITruthStreamCapabilities? _capabilities;

    /// <summary>
    /// Initializes a new instance of PlateTopologyMaterializer.
    /// </summary>
    /// <param name="store">The event store to read events from.</param>
    public PlateTopologyMaterializer(ITopologyEventStore store)
        : this(store, store as ITruthStreamCapabilities)
    {
    }

    /// <summary>
    /// Initializes a new instance of PlateTopologyMaterializer with explicit capabilities.
    /// </summary>
    /// <param name="store">The event store to read events from.</param>
    /// <param name="capabilities">
    /// Optional stream capabilities provider for tick-based optimization.
    /// If null, Auto mode behaves like ScanAll.
    /// </param>
    public PlateTopologyMaterializer(ITopologyEventStore store, ITruthStreamCapabilities? capabilities)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _capabilities = capabilities;
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

        state.RebuildIndices();

        return state;
    }

    /// <summary>
    /// Materializes topology state up to and including a target sequence number.
    ///
    /// This is the sequence-based cutoff: applies events where event.Sequence &lt;= targetSequence.
    /// Use this for deterministic replay or debugging specific event prefixes.
    /// </summary>
    /// <param name="stream">The truth stream identity to materialize.</param>
    /// <param name="targetSequence">The maximum sequence number to include (inclusive).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The materialized topology state.</returns>
    public async Task<PlateTopologyState> MaterializeAtSequenceAsync(
        TruthStreamIdentity stream,
        long targetSequence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (targetSequence < -1)
            throw new ArgumentOutOfRangeException(nameof(targetSequence), "Sequence must be >= -1");

        var state = new PlateTopologyState(stream);
        if (targetSequence < 0)
            return state;

        await foreach (var evt in _store.ReadAsync(stream, 0, cancellationToken))
        {
            // Events are in sequence order, so we can break early
            if (evt.Sequence > targetSequence)
                break;

            ApplyEvent(state, evt);
        }

        state.RebuildIndices();

        return state;
    }

    /// <summary>
    /// Materializes topology state up to and including a target tick (simulation time).
    ///
    /// This is the tick-based cutoff: applies events where event.Tick &lt;= targetTick.
    /// Use this for "what did the world look like at tick X?" queries.
    ///
    /// The mode parameter controls iteration behavior:
    /// - ScanAll: Scans all events (always correct, but slower)
    /// - BreakOnFirstBeyondTick: Breaks early (fast, but only safe for monotone streams)
    /// - Auto: Queries stream capabilities and chooses the best mode
    ///
    /// When in doubt, use Auto (the default). It will only optimize when safe.
    /// </summary>
    /// <param name="stream">The truth stream identity to materialize.</param>
    /// <param name="targetTick">The maximum tick to include (inclusive).</param>
    /// <param name="mode">Controls iteration behavior. Default is Auto.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The materialized topology state containing entities at or before the target tick.</returns>
    public async Task<PlateTopologyState> MaterializeAtTickAsync(
        TruthStreamIdentity stream,
        CanonicalTick targetTick,
        TickMaterializationMode mode = TickMaterializationMode.Auto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Determine effective mode
        var effectiveMode = await ResolveEffectiveModeAsync(stream, mode, cancellationToken);

        var state = new PlateTopologyState(stream);

        await foreach (var evt in _store.ReadAsync(stream, 0, cancellationToken))
        {
            if (effectiveMode == TickMaterializationMode.BreakOnFirstBeyondTick)
            {
                // Fast path: break early (only safe for monotone streams)
                if (evt.Tick > targetTick)
                    break;

                ApplyEvent(state, evt);
            }
            else
            {
                // Safe path: scan all, filter by tick
                if (evt.Tick <= targetTick)
                {
                    ApplyEvent(state, evt);
                }
                // Continue scanning - later events may have earlier ticks
            }
        }

        state.RebuildIndices();

        return state;
    }

    /// <summary>
    /// Resolves the effective materialization mode based on stream capabilities.
    /// </summary>
    private async ValueTask<TickMaterializationMode> ResolveEffectiveModeAsync(
        TruthStreamIdentity stream,
        TickMaterializationMode requestedMode,
        CancellationToken cancellationToken)
    {
        // Explicit modes are used as-is
        if (requestedMode != TickMaterializationMode.Auto)
            return requestedMode;

        // Auto: check stream capabilities
        if (_capabilities == null)
        {
            Trace.WriteLineIf(
                DiagnosticSwitches.MaterializationOptimization.TraceInfo,
                $"[Materializer] Auto resolved to ScanAll for {stream} (no capabilities provider)");
            return TickMaterializationMode.ScanAll;
        }

        var isMonotone = await _capabilities.IsTickMonotoneFromGenesisAsync(stream, cancellationToken);

        if (isMonotone)
        {
            Trace.WriteLineIf(
                DiagnosticSwitches.MaterializationOptimization.TraceInfo,
                $"[Materializer] Auto resolved to BreakOnFirstBeyondTick for {stream} (TickMonotoneFromGenesis=true)");
            return TickMaterializationMode.BreakOnFirstBeyondTick;
        }
        else
        {
            Trace.WriteLineIf(
                DiagnosticSwitches.MaterializationOptimization.TraceInfo,
                $"[Materializer] Auto resolved to ScanAll for {stream} (TickMonotoneFromGenesis=false)");
            return TickMaterializationMode.ScanAll;
        }
    }

    /// <summary>
    /// [OBSOLETE] Use MaterializeAtSequenceAsync or MaterializeAtTickAsync instead.
    /// This method is kept for backward compatibility but delegates to MaterializeAtSequenceAsync.
    /// </summary>
    [Obsolete("Use MaterializeAtSequenceAsync (for sequence cutoff) or MaterializeAtTickAsync (for tick cutoff) instead.")]
    public Task<PlateTopologyState> MaterializeAtTickAsync(
        TruthStreamIdentity stream,
        long tick,
        CancellationToken cancellationToken = default)
    {
        // Old behavior was sequence-based despite the name
        return MaterializeAtSequenceAsync(stream, tick, cancellationToken);
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

        state.RebuildIndices();

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
