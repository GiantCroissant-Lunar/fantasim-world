using System.Linq;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Derived;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;

namespace Plate.Topology.Tests.Integration;

/// <summary>
/// Tests for tick-based materialization cutoff behavior.
///
/// These tests verify that MaterializeAtTickAsync correctly applies events
/// based on their tick value, not sequence number. This is critical for
/// simulation time queries ("what did the world look like at tick X?").
/// </summary>
public sealed class TickCutoffMaterializationTests
{
    private readonly TruthStreamIdentity _stream = new(
        "science",
        "main",
        2,
        Domain.Parse("geo.plates"),
        "0");

    #region Tick Cutoff Tests

    /// <summary>
    /// Verifies that tick-based materialization includes all events up to the target tick.
    /// Events: seq 0 tick 10, seq 1 tick 20, seq 2 tick 30
    /// Query at tick 20 → should include seq 0 and seq 1 only
    /// </summary>
    [Fact]
    public async Task TickCutoff_IncludesAllEventsUpToTick()
    {
        // Arrange
        var store = new InMemoryTopologyEventStore();
        var plate1 = new PlateId(Guid.NewGuid());
        var plate2 = new PlateId(Guid.NewGuid());
        var plate3 = new PlateId(Guid.NewGuid());

        await store.AppendAsync(
            _stream,
            new IPlateTopologyEvent[]
            {
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate1, new CanonicalTick(10), 0, _stream),
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate2, new CanonicalTick(20), 1, _stream),
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate3, new CanonicalTick(30), 2, _stream),
            },
            CancellationToken.None);

        var materializer = new PlateTopologyMaterializer(store);

        // Act - Materialize at tick 20
        var state = await materializer.MaterializeAtTickAsync(_stream, new CanonicalTick(20), CancellationToken.None);

        // Assert - Should have plates 1 and 2 (ticks 10 and 20), but not plate 3 (tick 30)
        Assert.Equal(2, state.Plates.Count);
        Assert.True(state.Plates.ContainsKey(plate1));
        Assert.True(state.Plates.ContainsKey(plate2));
        Assert.False(state.Plates.ContainsKey(plate3));
    }

    /// <summary>
    /// Verifies that tick-based materialization does NOT assume monotone ticks.
    /// Events: seq 0 tick 10, seq 1 tick 30, seq 2 tick 20
    /// Query at tick 20 → should include seq 0 and seq 2 (NOT seq 1 because tick 30 > 20)
    ///
    /// This proves we scan all events and don't break early on first tick > target.
    /// </summary>
    [Fact]
    public async Task TickCutoff_DoesNotAssumeMonotone()
    {
        // Arrange - Non-monotone ticks: 10, 30, 20
        var store = new InMemoryTopologyEventStore();
        var plate1 = new PlateId(Guid.NewGuid());
        var plate2 = new PlateId(Guid.NewGuid());
        var plate3 = new PlateId(Guid.NewGuid());

        await store.AppendAsync(
            _stream,
            new IPlateTopologyEvent[]
            {
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate1, new CanonicalTick(10), 0, _stream),
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate2, new CanonicalTick(30), 1, _stream),  // Beyond target
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate3, new CanonicalTick(20), 2, _stream),  // At target (after seq 1!)
            },
            CancellationToken.None);

        var materializer = new PlateTopologyMaterializer(store);

        // Act - Materialize at tick 20
        var state = await materializer.MaterializeAtTickAsync(_stream, new CanonicalTick(20), CancellationToken.None);

        // Assert - Should have plates 1 and 3 (ticks 10 and 20)
        // Should NOT have plate 2 (tick 30 > 20)
        Assert.Equal(2, state.Plates.Count);
        Assert.True(state.Plates.ContainsKey(plate1), "Plate1 (tick 10) should be included");
        Assert.False(state.Plates.ContainsKey(plate2), "Plate2 (tick 30) should NOT be included");
        Assert.True(state.Plates.ContainsKey(plate3), "Plate3 (tick 20) should be included - proves we didn't break early");
    }

    /// <summary>
    /// Verifies that sequence-based materialization differs from tick-based.
    /// Same events as above, but using sequence cutoff.
    /// </summary>
    [Fact]
    public async Task SequenceCutoff_DiffersFromTickCutoff()
    {
        // Arrange - Same non-monotone ticks as above
        var store = new InMemoryTopologyEventStore();
        var plate1 = new PlateId(Guid.NewGuid());
        var plate2 = new PlateId(Guid.NewGuid());
        var plate3 = new PlateId(Guid.NewGuid());

        await store.AppendAsync(
            _stream,
            new IPlateTopologyEvent[]
            {
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate1, new CanonicalTick(10), 0, _stream),
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate2, new CanonicalTick(30), 1, _stream),
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate3, new CanonicalTick(20), 2, _stream),
            },
            CancellationToken.None);

        var materializer = new PlateTopologyMaterializer(store);

        // Act - Materialize at sequence 1 (should include seq 0 and 1, but not seq 2)
        var state = await materializer.MaterializeAtSequenceAsync(_stream, 1, CancellationToken.None);

        // Assert - Should have plates 1 and 2 (sequences 0 and 1)
        // Should NOT have plate 3 (sequence 2)
        Assert.Equal(2, state.Plates.Count);
        Assert.True(state.Plates.ContainsKey(plate1), "Plate1 (seq 0) should be included");
        Assert.True(state.Plates.ContainsKey(plate2), "Plate2 (seq 1) should be included");
        Assert.False(state.Plates.ContainsKey(plate3), "Plate3 (seq 2) should NOT be included");
    }

    #endregion

    #region Timeline Facade Tests

    /// <summary>
    /// Verifies that Timeline.GetLatestSlice equals materializing all events.
    /// </summary>
    [Fact]
    public async Task Timeline_GetLatest_EqualsMaterializeAll()
    {
        // Arrange
        var store = new InMemoryTopologyEventStore();
        var snapshotStore = new InMemorySnapshotStore();
        var plate1 = new PlateId(Guid.NewGuid());
        var plate2 = new PlateId(Guid.NewGuid());

        await store.AppendAsync(
            _stream,
            new IPlateTopologyEvent[]
            {
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate1, new CanonicalTick(10), 0, _stream),
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate2, new CanonicalTick(20), 1, _stream),
            },
            CancellationToken.None);

        var timeline = new PlateTopologyTimeline(store, snapshotStore);
        var materializer = new PlateTopologyMaterializer(store);

        // Act
        var latestSlice = await timeline.GetLatestSliceAsync(_stream, CancellationToken.None);
        var fullState = await materializer.MaterializeAsync(_stream, CancellationToken.None);

        // Assert - Same plates
        Assert.Equal(fullState.Plates.Count, latestSlice.State.Plates.Count);
        Assert.Equal(fullState.LastEventSequence, latestSlice.State.LastEventSequence);
    }

    /// <summary>
    /// Verifies that Timeline.GetSliceAtTick returns correct subset.
    /// </summary>
    [Fact]
    public async Task Timeline_GetSliceAtTick_ReturnsCorrectSubset()
    {
        // Arrange
        var store = new InMemoryTopologyEventStore();
        var snapshotStore = new InMemorySnapshotStore();
        var plate1 = new PlateId(Guid.NewGuid());
        var plate2 = new PlateId(Guid.NewGuid());
        var plate3 = new PlateId(Guid.NewGuid());

        await store.AppendAsync(
            _stream,
            new IPlateTopologyEvent[]
            {
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate1, new CanonicalTick(10), 0, _stream),
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate2, new CanonicalTick(20), 1, _stream),
                TestEventFactory.PlateCreated(Guid.NewGuid(), plate3, new CanonicalTick(30), 2, _stream),
            },
            CancellationToken.None);

        var timeline = new PlateTopologyTimeline(store, snapshotStore);

        // Act
        var sliceAt15 = await timeline.GetSliceAtTickAsync(_stream, new CanonicalTick(15), CancellationToken.None);
        var sliceAt25 = await timeline.GetSliceAtTickAsync(_stream, new CanonicalTick(25), CancellationToken.None);

        // Assert
        Assert.Equal(1, sliceAt15.State.Plates.Count);  // Only plate1 (tick 10)
        Assert.Equal(2, sliceAt25.State.Plates.Count);  // plate1 and plate2 (ticks 10, 20)
    }

    #endregion

    #region Test Helpers

    private sealed class InMemoryTopologyEventStore : ITopologyEventStore
    {
        private readonly List<IPlateTopologyEvent> _events = new();

        public Task AppendAsync(TruthStreamIdentity stream, IEnumerable<IPlateTopologyEvent> events, CancellationToken cancellationToken)
        {
            _events.AddRange(events);
            return Task.CompletedTask;
        }

        public Task AppendAsync(TruthStreamIdentity stream, IEnumerable<IPlateTopologyEvent> events, AppendOptions options, CancellationToken cancellationToken)
        {
            return AppendAsync(stream, events, cancellationToken);
        }

        public async IAsyncEnumerable<IPlateTopologyEvent> ReadAsync(
            TruthStreamIdentity stream,
            long fromSequenceInclusive,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            foreach (var e in _events.Where(e => e.StreamIdentity == stream && e.Sequence >= fromSequenceInclusive).OrderBy(e => e.Sequence))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return e;
            }
        }

        public Task<long?> GetLastSequenceAsync(TruthStreamIdentity stream, CancellationToken cancellationToken)
        {
            var last = _events.Where(e => e.StreamIdentity == stream).Select(e => (long?)e.Sequence).DefaultIfEmpty(null).Max();
            return Task.FromResult(last);
        }
    }

    private sealed class InMemorySnapshotStore : IPlateTopologySnapshotStore
    {
        private readonly Dictionary<PlateTopologyMaterializationKey, PlateTopologySnapshot> _snapshots = new();

        public Task SaveSnapshotAsync(PlateTopologySnapshot snapshot, CancellationToken cancellationToken)
        {
            _snapshots[snapshot.Key] = snapshot;
            return Task.CompletedTask;
        }

        public Task<PlateTopologySnapshot?> GetSnapshotAsync(PlateTopologyMaterializationKey key, CancellationToken cancellationToken)
        {
            if (_snapshots.TryGetValue(key, out var snapshot))
                return Task.FromResult<PlateTopologySnapshot?>(snapshot);
            return Task.FromResult<PlateTopologySnapshot?>(null);
        }
    }

    #endregion
}
