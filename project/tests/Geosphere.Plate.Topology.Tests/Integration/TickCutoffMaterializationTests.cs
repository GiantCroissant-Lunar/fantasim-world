using System.Linq;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Capabilities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Integration;

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
    /// Query at tick 20 ??should include seq 0 and seq 1 only
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
        var state = await materializer.MaterializeAtTickAsync(_stream, new CanonicalTick(20), cancellationToken: CancellationToken.None);

        // Assert - Should have plates 1 and 2 (ticks 10 and 20), but not plate 3 (tick 30)
        Assert.Equal(2, state.Plates.Count);
        Assert.True(state.Plates.ContainsKey(plate1));
        Assert.True(state.Plates.ContainsKey(plate2));
        Assert.False(state.Plates.ContainsKey(plate3));
    }

    /// <summary>
    /// Verifies that tick-based materialization does NOT assume monotone ticks.
    /// Events: seq 0 tick 10, seq 1 tick 30, seq 2 tick 20
    /// Query at tick 20 ??should include seq 0 and seq 2 (NOT seq 1 because tick 30 > 20)
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
        var state = await materializer.MaterializeAtTickAsync(_stream, new CanonicalTick(20), cancellationToken: CancellationToken.None);

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
        var sliceAt15 = await timeline.GetSliceAtTickAsync(_stream, new CanonicalTick(15), cancellationToken: CancellationToken.None);
        var sliceAt25 = await timeline.GetSliceAtTickAsync(_stream, new CanonicalTick(25), cancellationToken: CancellationToken.None);

        // Assert
        Assert.Equal(1, sliceAt15.State.Plates.Count);  // Only plate1 (tick 10)
        Assert.Equal(2, sliceAt25.State.Plates.Count);  // plate1 and plate2 (ticks 10, 20)
    }

    #endregion

    #region Snapshot Selection Tests (latest <= tick)

    /// <summary>
    /// Proves that GetLatestSnapshotBeforeAsync returns the largest tick <= target.
    /// </summary>
    [Fact]
    public async Task GetLatestSnapshotBefore_ReturnsLargestTickLessThanOrEqual()
    {
        // Arrange - snapshots at ticks 10, 20, 30
        var snapshotStore = new InMemorySnapshotStore();
        var state10 = new PlateTopologyState(_stream);
        var state20 = new PlateTopologyState(_stream);
        var state30 = new PlateTopologyState(_stream);

        await snapshotStore.SaveSnapshotAsync(new PlateTopologySnapshot(
            Key: new PlateTopologyMaterializationKey(_stream, 10),
            LastEventSequence: 10,
            Plates: Array.Empty<PlateEntity>(),
            Boundaries: Array.Empty<Boundary>(),
            Junctions: Array.Empty<Junction>()), CancellationToken.None);
        await snapshotStore.SaveSnapshotAsync(new PlateTopologySnapshot(
            Key: new PlateTopologyMaterializationKey(_stream, 20),
            LastEventSequence: 20,
            Plates: Array.Empty<PlateEntity>(),
            Boundaries: Array.Empty<Boundary>(),
            Junctions: Array.Empty<Junction>()), CancellationToken.None);
        await snapshotStore.SaveSnapshotAsync(new PlateTopologySnapshot(
            Key: new PlateTopologyMaterializationKey(_stream, 30),
            LastEventSequence: 30,
            Plates: Array.Empty<PlateEntity>(),
            Boundaries: Array.Empty<Boundary>(),
            Junctions: Array.Empty<Junction>()), CancellationToken.None);

        // Act & Assert - query at various ticks
        var at5 = await snapshotStore.GetLatestSnapshotBeforeAsync(_stream, 5, CancellationToken.None);
        var at10 = await snapshotStore.GetLatestSnapshotBeforeAsync(_stream, 10, CancellationToken.None);
        var at15 = await snapshotStore.GetLatestSnapshotBeforeAsync(_stream, 15, CancellationToken.None);
        var at25 = await snapshotStore.GetLatestSnapshotBeforeAsync(_stream, 25, CancellationToken.None);
        var at100 = await snapshotStore.GetLatestSnapshotBeforeAsync(_stream, 100, CancellationToken.None);

        Assert.Null(at5);  // No snapshot at or before tick 5
        Assert.NotNull(at10);
        Assert.Equal(10, at10!.Value.Key.Tick);  // Exact match
        Assert.NotNull(at15);
        Assert.Equal(10, at15!.Value.Key.Tick);  // Largest <= 15 is 10
        Assert.NotNull(at25);
        Assert.Equal(20, at25!.Value.Key.Tick);  // Largest <= 25 is 20
        Assert.NotNull(at100);
        Assert.Equal(30, at100!.Value.Key.Tick); // Largest <= 100 is 30
    }

    /// <summary>
    /// Proves that GetLatestSnapshotBeforeAsync respects stream boundaries.
    /// Snapshots from other streams should not be returned.
    /// </summary>
    [Fact]
    public async Task GetLatestSnapshotBefore_RespectsStreamBoundaries()
    {
        // Arrange - two different streams
        var stream1 = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "0");
        var stream2 = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "1"); // Different model

        var snapshotStore = new InMemorySnapshotStore();
        var state1 = new PlateTopologyState(stream1);
        var state2 = new PlateTopologyState(stream2);

        await snapshotStore.SaveSnapshotAsync(new PlateTopologySnapshot(
            Key: new PlateTopologyMaterializationKey(stream1, 10),
            LastEventSequence: 10,
            Plates: Array.Empty<PlateEntity>(),
            Boundaries: Array.Empty<Boundary>(),
            Junctions: Array.Empty<Junction>()), CancellationToken.None);
        await snapshotStore.SaveSnapshotAsync(new PlateTopologySnapshot(
            Key: new PlateTopologyMaterializationKey(stream2, 50),
            LastEventSequence: 50,
            Plates: Array.Empty<PlateEntity>(),
            Boundaries: Array.Empty<Boundary>(),
            Junctions: Array.Empty<Junction>()), CancellationToken.None);

        // Act - query stream1 at tick 100
        var result = await snapshotStore.GetLatestSnapshotBeforeAsync(stream1, 100, CancellationToken.None);

        // Assert - should get stream1's snapshot at 10, NOT stream2's at 50
        Assert.NotNull(result);
        Assert.Equal(10, result!.Value.Key.Tick);
        Assert.Equal(stream1, result.Value.Key.Stream);
    }

    #endregion

    #region Incremental Replay Tests

    /// <summary>
    /// Proves that incremental replay skips events before the snapshot sequence.
    /// This verifies the store is read from startSeq = snapshot.LastEventSequence + 1.
    /// </summary>
    [Fact]
    public async Task IncrementalReplay_SkipsPrefixBySequence()
    {
        // Arrange - append 10 events at ticks 0..9
        var store = new CountingEventStore();
        var snapshotStore = new InMemorySnapshotStore();
        var plates = Enumerable.Range(0, 10)
            .Select(i => new PlateId(Guid.NewGuid()))
            .ToArray();

        await store.AppendAsync(
            _stream,
            plates.Select((p, i) => (IPlateTopologyEvent)TestEventFactory.PlateCreated(
                Guid.NewGuid(), p, new CanonicalTick(i), i, _stream)),
            CancellationToken.None);

        // Create snapshot at seq 5 (covers events 0..5, i.e., 6 events)
        var stateAtSeq5 = new PlateTopologyState(_stream);
        for (var i = 0; i <= 5; i++)
            stateAtSeq5.Plates[plates[i]] = new FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate(plates[i], false, null);

        await snapshotStore.SaveSnapshotAsync(new PlateTopologySnapshot(
            new PlateTopologyMaterializationKey(_stream, 5),  // tick 5, seq 5
            5,  // LastEventSequence
            stateAtSeq5.Plates.Values.ToArray(),
            Array.Empty<Boundary>(),
            Array.Empty<Junction>()), CancellationToken.None);

        var materializer = new SnapshottingPlateTopologyMaterializer(store, snapshotStore);

        // Act - materialize at tick 9 (max tick)
        store.ResetReadCount();
        var result = await materializer.MaterializeAtTickAsync(
            _stream,
            new CanonicalTick(9),
            TickMaterializationMode.ScanAll,
            CancellationToken.None);

        // Assert - should have read only events 6..9 (4 events), not 0..9 (10 events)
        Assert.True(result.FromSnapshot, "Should have used snapshot");
        Assert.Equal(10, result.State.Plates.Count); // All 10 plates present
        Assert.Equal(4, store.EventsRead); // Only events 6,7,8,9 were read
    }

    /// <summary>
    /// Proves that incremental replay does NOT miss back-in-time events.
    /// This is the critical correctness test: sequence boundary, not tick boundary.
    /// </summary>
    [Fact]
    public async Task IncrementalReplay_DoesNotMissBackInTimeEvent()
    {
        // Arrange - create scenario where event arrives "back in time"
        // Snapshot at tick 1000, seq 10
        // Then append event at seq 11 with tick 900 (back-in-time!)
        var store = new InMemoryTopologyEventStore();
        var snapshotStore = new InMemorySnapshotStore();

        var plate1 = new PlateId(Guid.NewGuid());
        var plate2 = new PlateId(Guid.NewGuid()); // This one arrives "late" with earlier tick

        // First, append 11 events ending at seq 10 tick 1000
        var initialEvents = Enumerable.Range(0, 11)
            .Select(i => TestEventFactory.PlateCreated(
                Guid.NewGuid(),
                i == 0 ? plate1 : new PlateId(Guid.NewGuid()),
                new CanonicalTick(i * 100),  // ticks: 0, 100, 200, ... 1000
                i,
                _stream))
            .ToList();
        await store.AppendAsync(_stream, initialEvents.Cast<IPlateTopologyEvent>(), CancellationToken.None);

        // Create snapshot at tick 1000 (last tick), seq 10
        var stateAtSnapshot = new PlateTopologyState(_stream);
        foreach (var evt in initialEvents.Cast<PlateCreatedEvent>())
            stateAtSnapshot.Plates[evt.PlateId] = new FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate(evt.PlateId, false, null);

        await snapshotStore.SaveSnapshotAsync(new PlateTopologySnapshot(
            new PlateTopologyMaterializationKey(_stream, 1000),  // tick 1000
            10,  // LastEventSequence
            stateAtSnapshot.Plates.Values.ToArray(),
            Array.Empty<Boundary>(),
            Array.Empty<Junction>()), CancellationToken.None);

        // Now append a "back-in-time" event: seq 11 with tick 900
        await store.AppendAsync(
            _stream,
            new IPlateTopologyEvent[] { TestEventFactory.PlateCreated(Guid.NewGuid(), plate2, new CanonicalTick(900), 11, _stream) },
            CancellationToken.None);

        var materializer = new SnapshottingPlateTopologyMaterializer(store, snapshotStore);

        // Act - query at tick 1000
        // If we incorrectly used tick boundary, we'd miss plate2 (tick 900 < 1000 but seq 11 > 10)
        // If we correctly use sequence boundary, we'll read seq 11 and include it (tick 900 <= 1000)
        var result = await materializer.MaterializeAtTickAsync(
            _stream,
            new CanonicalTick(1000),
            TickMaterializationMode.ScanAll,  // Must be ScanAll to catch back-in-time events
            CancellationToken.None);

        // Assert - result MUST include plate2 (the back-in-time event)
        Assert.True(result.FromSnapshot, "Should have used snapshot");
        Assert.True(result.State.Plates.ContainsKey(plate2),
            "CRITICAL: Must include back-in-time event (seq 11, tick 900). " +
            "This proves we use sequence boundary, not tick boundary.");
        Assert.Equal(12, result.State.Plates.Count); // 11 from snapshot + 1 back-in-time
    }

    #endregion

    #region TickMaterializationMode Tests

    /// <summary>
    /// Proves that Auto mode does NOT break early when capabilities returns false.
    /// This is the safety test: even with non-monotone ticks, we get correct results.
    /// </summary>
    [Fact]
    public async Task Auto_DoesNotBreak_WhenNotMonotone()
    {
        // Arrange - Non-monotone ticks: 10, 30, 20
        var store = new InMemoryTopologyEventStore();
        var capabilities = new FakeCapabilities(isMonotone: false);
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

        var materializer = new PlateTopologyMaterializer(store, capabilities);

        // Act - Auto mode should use ScanAll because capabilities.IsTickMonotone == false
        var state = await materializer.MaterializeAtTickAsync(
            _stream,
            new CanonicalTick(20),
            TickMaterializationMode.Auto,
            CancellationToken.None);

        // Assert - Should have plates 1 and 3 (ticks 10 and 20)
        // Plate3 proves we didn't break early at plate2 (tick 30)
        Assert.Equal(2, state.Plates.Count);
        Assert.True(state.Plates.ContainsKey(plate1), "Plate1 (tick 10) should be included");
        Assert.False(state.Plates.ContainsKey(plate2), "Plate2 (tick 30) should NOT be included");
        Assert.True(state.Plates.ContainsKey(plate3), "Plate3 (tick 20) should be included - proves ScanAll was used");
    }

    /// <summary>
    /// Proves that BreakOnFirstBeyondTick mode stops early when ticks ARE monotone.
    /// This verifies the optimization works.
    /// </summary>
    [Fact]
    public async Task BreakMode_BreaksEarly_WhenMonotone()
    {
        // Arrange - Monotone ticks: 10, 20, 30
        var store = new CountingEventStore();
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

        var materializer = new PlateTopologyMaterializer(store, null);

        // Act - Force BreakOnFirstBeyondTick mode
        store.ResetReadCount();
        var state = await materializer.MaterializeAtTickAsync(
            _stream,
            new CanonicalTick(20),
            TickMaterializationMode.BreakOnFirstBeyondTick,
            CancellationToken.None);

        // Assert - Should have plates 1 and 2
        Assert.Equal(2, state.Plates.Count);
        Assert.True(state.Plates.ContainsKey(plate1));
        Assert.True(state.Plates.ContainsKey(plate2));
        Assert.False(state.Plates.ContainsKey(plate3));

        // Should have read exactly 3 events (read plate3, saw tick 30 > 20, broke)
        Assert.Equal(3, store.EventsRead);
    }

    /// <summary>
    /// Proves that ScanAll mode reads all events even when ticks are monotone.
    /// </summary>
    [Fact]
    public async Task ScanAll_ReadsAllEvents_EvenWhenMonotone()
    {
        // Arrange - Monotone ticks: 10, 20, 30, 40, 50
        var store = new CountingEventStore();
        var plates = Enumerable.Range(0, 5).Select(_ => new PlateId(Guid.NewGuid())).ToArray();

        await store.AppendAsync(
            _stream,
            plates.Select((p, i) => (IPlateTopologyEvent)TestEventFactory.PlateCreated(
                Guid.NewGuid(), p, new CanonicalTick((i + 1) * 10), i, _stream)),
            CancellationToken.None);

        var materializer = new PlateTopologyMaterializer(store, null);

        // Act - Force ScanAll mode
        store.ResetReadCount();
        var state = await materializer.MaterializeAtTickAsync(
            _stream,
            new CanonicalTick(20),
            TickMaterializationMode.ScanAll,
            CancellationToken.None);

        // Assert - Should have plates 0 and 1 (ticks 10, 20)
        Assert.Equal(2, state.Plates.Count);

        // Should have read ALL 5 events (ScanAll doesn't break early)
        Assert.Equal(5, store.EventsRead);
    }

    /// <summary>
    /// Proves that Auto mode uses BreakOnFirstBeyondTick when capabilities says monotone.
    /// </summary>
    [Fact]
    public async Task Auto_UsesBreak_WhenCapabilitiesSaysMonotone()
    {
        // Arrange - Monotone ticks: 10, 20, 30, 40, 50
        var store = new CountingEventStore();
        var capabilities = new FakeCapabilities(isMonotone: true);
        var plates = Enumerable.Range(0, 5).Select(_ => new PlateId(Guid.NewGuid())).ToArray();

        await store.AppendAsync(
            _stream,
            plates.Select((p, i) => (IPlateTopologyEvent)TestEventFactory.PlateCreated(
                Guid.NewGuid(), p, new CanonicalTick((i + 1) * 10), i, _stream)),
            CancellationToken.None);

        var materializer = new PlateTopologyMaterializer(store, capabilities);

        // Act - Auto should choose BreakOnFirstBeyondTick
        store.ResetReadCount();
        var state = await materializer.MaterializeAtTickAsync(
            _stream,
            new CanonicalTick(20),
            TickMaterializationMode.Auto,
            CancellationToken.None);

        // Assert - Should have plates 0 and 1
        Assert.Equal(2, state.Plates.Count);

        // Should have broken early - read only 3 events (10, 20, then saw 30 and broke)
        Assert.Equal(3, store.EventsRead);
    }

    #endregion

    #region Test Helpers

    private sealed class FakeCapabilities : ITruthStreamCapabilities
    {
        private readonly bool _isMonotone;

        public FakeCapabilities(bool isMonotone) => _isMonotone = isMonotone;

        public ValueTask<bool> IsTickMonotoneFromGenesisAsync(
            TruthStreamIdentity stream,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_isMonotone);
        }
    }

    private sealed class CountingEventStore : ITopologyEventStore
    {
        private readonly List<IPlateTopologyEvent> _events = new();
        public int EventsRead { get; private set; }

        public void ResetReadCount() => EventsRead = 0;

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
                EventsRead++;
                yield return e;
            }
        }

        public Task<long?> GetLastSequenceAsync(TruthStreamIdentity stream, CancellationToken cancellationToken)
        {
            var last = _events.Where(e => e.StreamIdentity == stream).Select(e => (long?)e.Sequence).DefaultIfEmpty(null).Max();
            return Task.FromResult(last);
        }
    }

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

        public Task<PlateTopologySnapshot?> GetLatestSnapshotBeforeAsync(
            TruthStreamIdentity stream,
            long targetTick,
            CancellationToken cancellationToken)
        {
            // Simple O(n) implementation for tests - find largest tick <= targetTick
            PlateTopologySnapshot? best = null;
            foreach (var kvp in _snapshots)
            {
                if (kvp.Key.Stream == stream && kvp.Key.Tick <= targetTick)
                {
                    if (best == null || kvp.Key.Tick > best.Value.Key.Tick)
                        best = kvp.Value;
                }
            }
            return Task.FromResult(best);
        }
    }

    #endregion
}
