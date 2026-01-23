using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Capabilities;
using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Materializer;

/// <summary>
/// High-level facade for querying plate topology at specific points in time.
///
/// This is the recommended entry point for downstream consumers (e.g., Godot UI,
/// time sliders, simulation queries). It provides a clean API that hides the
/// complexity of snapshotting and caching.
///
/// Key methods:
/// - GetSliceAtTickAsync: Get topology state at a specific simulation tick
/// - GetLatestSliceAsync: Get the most recent topology state
/// - GetSliceAtSequenceAsync: Get topology state at a specific event sequence (for debugging)
/// </summary>
public sealed class PlateTopologyTimeline
{
    private readonly SnapshottingPlateTopologyMaterializer _materializer;

    /// <summary>
    /// Creates a timeline facade with snapshotting support.
    /// </summary>
    /// <param name="eventStore">The event store to read events from.</param>
    /// <param name="snapshotStore">The snapshot store for acceleration.</param>
    public PlateTopologyTimeline(
        ITopologyEventStore eventStore,
        IPlateTopologySnapshotStore snapshotStore)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(snapshotStore);

        _materializer = new SnapshottingPlateTopologyMaterializer(eventStore, snapshotStore);
    }

    /// <summary>
    /// Gets the topology state at a specific simulation tick.
    ///
    /// Returns the state containing all entities with tick &lt;= targetTick.
    /// This is the correct method for "what did the world look like at tick X?" queries.
    /// </summary>
    /// <param name="stream">The truth stream identity.</param>
    /// <param name="targetTick">The target simulation tick.</param>
    /// <param name="mode">Tick materialization mode (default: Auto).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only view of the topology state at the target tick.</returns>
    public async Task<TimelineSlice> GetSliceAtTickAsync(
        TruthStreamIdentity stream,
        CanonicalTick targetTick,
        TickMaterializationMode mode = TickMaterializationMode.Auto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var result = await _materializer.MaterializeAtTickAsync(stream, targetTick, mode, cancellationToken);
        return new TimelineSlice(
            stream,
            targetTick,
            result.State,
            result.FromSnapshot);
    }

    /// <summary>
    /// Gets the latest (most recent) topology state.
    ///
    /// Equivalent to GetSliceAtTickAsync with CanonicalTick.MaxValue.
    /// </summary>
    /// <param name="stream">The truth stream identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only view of the latest topology state.</returns>
    public Task<TimelineSlice> GetLatestSliceAsync(
        TruthStreamIdentity stream,
        CancellationToken cancellationToken = default)
    {
        return GetSliceAtTickAsync(stream, CanonicalTick.MaxValue, TickMaterializationMode.Auto, cancellationToken);
    }

    /// <summary>
    /// Gets the topology state at a specific event sequence number.
    ///
    /// This is a debugging/replay method that uses sequence-based cutoff instead
    /// of tick-based cutoff. Use GetSliceAtTickAsync for simulation queries.
    /// </summary>
    /// <param name="stream">The truth stream identity.</param>
    /// <param name="targetSequence">The target sequence number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only view of the topology state at the target sequence.</returns>
    public async Task<TimelineSlice> GetSliceAtSequenceAsync(
        TruthStreamIdentity stream,
        long targetSequence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var result = await _materializer.MaterializeAtSequenceAsync(stream, targetSequence, cancellationToken);

        // For sequence-based queries, use the sequence as the tick value in the slice
        var tick = new CanonicalTick(Math.Max(0, targetSequence));
        return new TimelineSlice(
            stream,
            tick,
            result.State,
            result.FromSnapshot);
    }

    /// <summary>
    /// Represents a topology slice at a specific point in time.
    /// </summary>
    /// <param name="Stream">The truth stream identity.</param>
    /// <param name="Tick">The tick at which this slice was materialized.</param>
    /// <param name="State">The topology state (read-only view recommended).</param>
    /// <param name="FromSnapshot">True if the slice was loaded from a snapshot.</param>
    public readonly record struct TimelineSlice(
        TruthStreamIdentity Stream,
        CanonicalTick Tick,
        IPlateTopologyIndexedStateView State,
        bool FromSnapshot);
}
