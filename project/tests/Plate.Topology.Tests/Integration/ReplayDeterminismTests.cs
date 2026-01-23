using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;

namespace Plate.Topology.Tests.Integration;

/// <summary>
/// Integration tests for PlateTopologyEventStore validating:
/// - Append and readback correctness
/// - Deterministic replay behavior
/// - Stream isolation across different TruthStreamIdentity instances
///
/// Per T0401: Validates append+readback, determinism, and stream isolation.
/// </summary>
public class ReplayDeterminismTests : IDisposable
{
    private readonly PlateTopologyEventStore _store;
    private readonly TruthStreamIdentity _stream1;
    private readonly TruthStreamIdentity _stream2;

    public ReplayDeterminismTests()
    {
        _store = TestStores.CreateEventStore();

        // Create two different stream identities for isolation testing
        _stream1 = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0"
        );

        _stream2 = new TruthStreamIdentity(
            "wuxing",
            "alternate",
            2,
            Domain.Parse("geo.plates"),
            "0"
        );
    }

    public void Dispose()
    {
        _store.Dispose();
    }

    [Fact]
    public async Task AppendAndReadback_SingleEvent_RetrievesCorrectEvent()
    {
        // Arrange
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var evt = new PlateCreatedEvent(
            Guid.NewGuid(),
            new PlateId(Guid.NewGuid()),
            timestamp,
            0,
            _stream1
        );

        // Act
        await _store.AppendAsync(_stream1, new IPlateTopologyEvent[] { evt }, CancellationToken.None);
        var events = await _store.ReadAsync(_stream1, 0, CancellationToken.None).ToListAsync();

        // Assert
        var retrieved = Assert.IsType<PlateCreatedEvent>(Assert.Single(events));
        Assert.Equal(evt.EventId, retrieved.EventId);
        Assert.Equal(evt.PlateId, retrieved.PlateId);
        Assert.Equal(evt.Timestamp, retrieved.Timestamp);
        Assert.Equal(evt.Sequence, retrieved.Sequence);
        Assert.Equal(evt.StreamIdentity, retrieved.StreamIdentity);
    }

    [Fact]
    public async Task AppendAndReadback_MultipleEvents_RetrievesInOrder()
    {
        // Arrange
        var plateIdLeft = new PlateId(Guid.NewGuid());
        var plateIdRight = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(
                Guid.NewGuid(),
                plateIdLeft,
                DateTimeOffset.UtcNow,
                0,
                _stream1
            ),
            new PlateCreatedEvent(
                Guid.NewGuid(),
                plateIdRight,
                DateTimeOffset.UtcNow,
                1,
                _stream1
            ),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateIdLeft,
                plateIdRight,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                DateTimeOffset.UtcNow,
                2,
                _stream1
            ),
            new BoundaryTypeChangedEvent(
                Guid.NewGuid(),
                boundaryId,
                BoundaryType.Transform,
                BoundaryType.Convergent,
                DateTimeOffset.UtcNow,
                3,
                _stream1
            )
        };

        // Act
        await _store.AppendAsync(_stream1, events, CancellationToken.None);
        var retrieved = await _store.ReadAsync(_stream1, 0, CancellationToken.None).ToListAsync();

        // Assert
        Assert.Equal(4, retrieved.Count);
        Assert.IsType<PlateCreatedEvent>(retrieved[0]);
        Assert.IsType<PlateCreatedEvent>(retrieved[1]);
        Assert.IsType<BoundaryCreatedEvent>(retrieved[2]);
        Assert.IsType<BoundaryTypeChangedEvent>(retrieved[3]);
        Assert.Equal(0, retrieved[0].Sequence);
        Assert.Equal(1, retrieved[1].Sequence);
        Assert.Equal(2, retrieved[2].Sequence);
        Assert.Equal(3, retrieved[3].Sequence);
        Assert.Equal(2, retrieved[2].Sequence);
    }

    [Fact]
    public async Task AppendAndReadback_RangeQuery_ReturnsOnlyRequestedRange()
    {
        // Arrange
        var plateId = new PlateId(Guid.NewGuid());
        var events = new List<IPlateTopologyEvent>();
        for (int i = 0; i < 10; i++)
        {
            events.Add(new PlateCreatedEvent(
                Guid.NewGuid(),
                new PlateId(Guid.NewGuid()),
                DateTimeOffset.UtcNow,
                i,
                _stream1
            ));
        }
        await _store.AppendAsync(_stream1, events, CancellationToken.None);

        // Act - Read from sequence 5 onwards
        var retrieved = await _store.ReadAsync(_stream1, 5, CancellationToken.None).ToListAsync();

        // Assert - Should get events 5, 6, 7, 8, 9 (5 events)
        Assert.Equal(5, retrieved.Count);
        Assert.Equal(5, retrieved[0].Sequence);
        Assert.Equal(9, retrieved[^1].Sequence);
    }

    [Fact]
    public async Task GetLastSequence_EmptyStream_ReturnsNull()
    {
        // Act
        var lastSeq = await _store.GetLastSequenceAsync(_stream1, CancellationToken.None);

        // Assert
        Assert.Null(lastSeq);
    }

    [Fact]
    public async Task GetLastSequence_AfterAppend_ReturnsLastSequence()
    {
        // Arrange
        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 0, _stream1),
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 1, _stream1),
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 5, _stream1)
        };

        await _store.AppendAsync(_stream1, events, CancellationToken.None);

        // Act
        var lastSeq = await _store.GetLastSequenceAsync(_stream1, CancellationToken.None);

        // Assert
        Assert.Equal(5, lastSeq);
    }

    [Fact]
    public async Task DeterministicReplay_MultipleReads_ReturnsIdenticalResults()
    {
        // Arrange
        var plateId = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId, DateTimeOffset.UtcNow, 0, _stream1),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateId,
                new PlateId(Guid.NewGuid()),
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                DateTimeOffset.UtcNow,
                1,
                _stream1
            ),
            new JunctionCreatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId],
                new Point2D(0.5, 0.0),
                DateTimeOffset.UtcNow,
                2,
                _stream1
            ),
            new BoundaryGeometryUpdatedEvent(
                Guid.NewGuid(),
                boundaryId,
                new LineSegment(0.0, 0.0, 2.0, 0.0),
                DateTimeOffset.UtcNow,
                3,
                _stream1
            ),
            new BoundaryTypeChangedEvent(
                Guid.NewGuid(),
                boundaryId,
                BoundaryType.Transform,
                BoundaryType.Convergent,
                DateTimeOffset.UtcNow,
                4,
                _stream1
            )
        };

        await _store.AppendAsync(_stream1, events, CancellationToken.None);

        // Act - Read the same stream multiple times
        var read1 = await _store.ReadAsync(_stream1, 0, CancellationToken.None).ToListAsync();
        var read2 = await _store.ReadAsync(_stream1, 0, CancellationToken.None).ToListAsync();
        var read3 = await _store.ReadAsync(_stream1, 0, CancellationToken.None).ToListAsync();

        // Assert - All reads should produce identical results
        Assert.Equal(read1.Count, read2.Count);
        Assert.Equal(read1.Count, read3.Count);

        for (int i = 0; i < read1.Count; i++)
        {
            Assert.Equal(read1[i].EventId, read2[i].EventId);
            Assert.Equal(read1[i].EventId, read3[i].EventId);
            Assert.Equal(read1[i].Sequence, read2[i].Sequence);
            Assert.Equal(read1[i].Sequence, read3[i].Sequence);
            Assert.Equal(read1[i].EventType, read2[i].EventType);
            Assert.Equal(read1[i].EventType, read3[i].EventType);
        }
    }

    [Fact]
    public async Task StreamIsolation_DifferentStreams_IndependentEvents()
    {
        // Arrange
        var events1 = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 0, _stream1),
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 1, _stream1)
        };

        var events2 = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 0, _stream2),
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 1, _stream2)
        };

        await _store.AppendAsync(_stream1, events1, CancellationToken.None);
        await _store.AppendAsync(_stream2, events2, CancellationToken.None);

        // Act
        var read1 = await _store.ReadAsync(_stream1, 0, CancellationToken.None).ToListAsync();
        var read2 = await _store.ReadAsync(_stream2, 0, CancellationToken.None).ToListAsync();

        // Assert
        Assert.Equal(2, read1.Count);
        Assert.Equal(2, read2.Count);

        // Verify stream isolation
        Assert.All(read1, e => Assert.Equal(_stream1, e.StreamIdentity));
        Assert.All(read2, e => Assert.Equal(_stream2, e.StreamIdentity));

        // Verify no cross-contamination
        var ids1 = read1.Select(e => e.EventId).ToImmutableHashSet();
        var ids2 = read2.Select(e => e.EventId).ToImmutableHashSet();
        Assert.Empty(ids1.Intersect(ids2));
    }

    [Fact]
    public async Task StreamIsolation_ReadFromDifferentStream_ReturnsEmpty()
    {
        // Arrange
        var evt = new PlateCreatedEvent(
            Guid.NewGuid(),
            new PlateId(Guid.NewGuid()),
            DateTimeOffset.UtcNow,
            0,
            _stream1
        );

        await _store.AppendAsync(_stream1, new IPlateTopologyEvent[] { evt }, CancellationToken.None);

        // Act - Read from a different stream that has no events
        var read = await _store.ReadAsync(_stream2, 0, CancellationToken.None).ToListAsync();

        // Assert
        Assert.Empty(read);
    }

    [Fact]
    public async Task Append_WithNonMonotonicSequence_ThrowsArgumentException()
    {
        // Arrange
        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 0, _stream1),
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 1, _stream1),
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 0, _stream1) // Invalid: not monotonic
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _store.AppendAsync(_stream1, events, CancellationToken.None)
        );
        Assert.Contains("monotonically increasing", exception.Message);
    }

    [Fact]
    public async Task Append_WithMismatchedStreamIdentity_ThrowsArgumentException()
    {
        // Arrange
        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 0, _stream1),
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 1, _stream2) // Wrong stream
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _store.AppendAsync(_stream1, events, CancellationToken.None)
        );
        Assert.Contains("does not match expected", exception.Message);
    }

    [Fact]
    public async Task Append_BatchOperation_IsAtomic()
    {
        // Arrange
        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 0, _stream1),
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 1, _stream1),
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 2, _stream1)
        };

        // Act
        await _store.AppendAsync(_stream1, events, CancellationToken.None);

        // If the batch wasn't atomic, we might see partial writes
        // Verify all three events are present
        var read = await _store.ReadAsync(_stream1, 0, CancellationToken.None).ToListAsync();

        // Assert
        Assert.Equal(3, read.Count);
        Assert.Equal(0, read[0].Sequence);
        Assert.Equal(1, read[1].Sequence);
        Assert.Equal(2, read[2].Sequence);
    }

    [Fact]
    public async Task DeterministicReplay_AllEventTypes_PreservesEventData()
    {
        // Arrange
        var plateId = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());
        var boundaryId2 = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            // Creation events
            new PlateCreatedEvent(
                Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
                plateId,
                new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero),
                0,
                _stream1
            ),
            new BoundaryCreatedEvent(
                Guid.Parse("11234567-89ab-cdef-0123-456789abcdef"),
                boundaryId,
                plateId,
                new PlateId(Guid.NewGuid()),
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new DateTimeOffset(2024, 1, 1, 12, 1, 0, TimeSpan.Zero),
                1,
                _stream1
            ),
            new JunctionCreatedEvent(
                Guid.Parse("21234567-89ab-cdef-0123-456789abcdef"),
                junctionId,
                [boundaryId],
                new Point2D(0.5, 0.0),
                new DateTimeOffset(2024, 1, 1, 12, 2, 0, TimeSpan.Zero),
                2,
                _stream1
            ),
            // Update events
            new BoundaryTypeChangedEvent(
                Guid.NewGuid(),
                boundaryId,
                BoundaryType.Transform,
                BoundaryType.Convergent,
                new DateTimeOffset(2024, 1, 1, 12, 3, 0, TimeSpan.Zero),
                3,
                _stream1
            )
        };

        // Act
        await _store.AppendAsync(_stream1, events, CancellationToken.None);
        var retrieved = await _store.ReadAsync(_stream1, 0, CancellationToken.None).ToListAsync();

        // Assert - All events preserved correctly
        Assert.Equal(4, retrieved.Count);

        var plateCreated = Assert.IsType<PlateCreatedEvent>(retrieved[0]);
        Assert.Equal(plateId, plateCreated.PlateId);
        Assert.Equal(0, plateCreated.Sequence);

        var boundaryCreated = Assert.IsType<BoundaryCreatedEvent>(retrieved[1]);
        Assert.Equal(boundaryId, boundaryCreated.BoundaryId);
        Assert.Equal(BoundaryType.Transform, boundaryCreated.BoundaryType);
        Assert.Equal(1, boundaryCreated.Sequence);

        var junctionCreated = Assert.IsType<JunctionCreatedEvent>(retrieved[2]);
        Assert.Equal(junctionId, junctionCreated.JunctionId);
        Assert.Single(junctionCreated.BoundaryIds);
        Assert.Equal(2, junctionCreated.Sequence);

        var typeChanged = Assert.IsType<BoundaryTypeChangedEvent>(retrieved[3]);
        Assert.Equal(BoundaryType.Transform, typeChanged.OldType);
        Assert.Equal(BoundaryType.Convergent, typeChanged.NewType);
        Assert.Equal(3, typeChanged.Sequence);
    }

    [Fact]
    public async Task Materialize_EmptyStream_ReturnsEmptyState()
    {
        // Arrange
        var materializer = new PlateTopologyMaterializer(_store);

        // Act - Materialize from empty stream
        var state = await materializer.MaterializeAsync(_stream1, CancellationToken.None);

        // Assert - Empty state with LastEventSequence = -1 (SC-008)
        Assert.Equal(_stream1, state.Identity);
        Assert.Empty(state.Plates);
        Assert.Empty(state.Boundaries);
        Assert.Empty(state.Junctions);
        Assert.Equal(-1, state.LastEventSequence);
        Assert.Empty(state.Violations);
    }

    [Fact]
    public async Task DeterministicReplay_RepeatMaterialization_ProducesIdenticalState()
    {
        // Arrange
        var plateId = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId, DateTimeOffset.UtcNow, 0, _stream1),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, DateTimeOffset.UtcNow, 1, _stream1),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateId,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                DateTimeOffset.UtcNow,
                2,
                _stream1
            ),
            new JunctionCreatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId],
                new Point2D(0.5, 0.0),
                DateTimeOffset.UtcNow,
                3,
                _stream1
            ),
            new BoundaryTypeChangedEvent(
                Guid.NewGuid(),
                boundaryId,
                BoundaryType.Transform,
                BoundaryType.Convergent,
                DateTimeOffset.UtcNow,
                4,
                _stream1
            ),
            new BoundaryGeometryUpdatedEvent(
                Guid.NewGuid(),
                boundaryId,
                new LineSegment(0.0, 0.0, 2.0, 0.0),
                DateTimeOffset.UtcNow,
                5,
                _stream1
            )
        };

        await _store.AppendAsync(_stream1, events, CancellationToken.None);
        var materializer = new PlateTopologyMaterializer(_store);

        // Act - Materialize the same stream multiple times
        var state1 = await materializer.MaterializeAsync(_stream1, CancellationToken.None);
        var state2 = await materializer.MaterializeAsync(_stream1, CancellationToken.None);
        var state3 = await materializer.MaterializeAsync(_stream1, CancellationToken.None);

        // Assert - All materializations should produce identical state (SC-001)
        Assert.Equal(state1.Identity, state2.Identity);
        Assert.Equal(state1.Identity, state3.Identity);

        Assert.Equal(state1.Plates.Count, state2.Plates.Count);
        Assert.Equal(state1.Plates.Count, state3.Plates.Count);
        Assert.Equal(2, state1.Plates.Count);

        Assert.Equal(state1.Boundaries.Count, state2.Boundaries.Count);
        Assert.Equal(state1.Boundaries.Count, state3.Boundaries.Count);
        Assert.Single(state1.Boundaries);

        Assert.Equal(state1.Junctions.Count, state2.Junctions.Count);
        Assert.Equal(state1.Junctions.Count, state3.Junctions.Count);
        Assert.Single(state1.Junctions);

        Assert.Equal(state1.LastEventSequence, state2.LastEventSequence);
        Assert.Equal(state1.LastEventSequence, state3.LastEventSequence);
        Assert.Equal(5, state1.LastEventSequence);

        // Verify plate data
        Assert.True(state1.Plates.ContainsKey(plateId));
        Assert.True(state2.Plates.ContainsKey(plateId));
        Assert.True(state3.Plates.ContainsKey(plateId));
        Assert.True(state1.Plates.ContainsKey(plateId2));
        Assert.True(state2.Plates.ContainsKey(plateId2));
        Assert.True(state3.Plates.ContainsKey(plateId2));
        Assert.Equal(state1.Plates[plateId], state2.Plates[plateId]);
        Assert.Equal(state1.Plates[plateId], state3.Plates[plateId]);

        // Verify boundary data
        Assert.True(state1.Boundaries.ContainsKey(boundaryId));
        Assert.True(state2.Boundaries.ContainsKey(boundaryId));
        Assert.True(state3.Boundaries.ContainsKey(boundaryId));
        Assert.Equal(state1.Boundaries[boundaryId], state2.Boundaries[boundaryId]);
        Assert.Equal(state1.Boundaries[boundaryId], state3.Boundaries[boundaryId]);
        Assert.Equal(BoundaryType.Convergent, state1.Boundaries[boundaryId].BoundaryType);

        // Verify junction data
        Assert.True(state1.Junctions.ContainsKey(junctionId));
        Assert.True(state2.Junctions.ContainsKey(junctionId));
        Assert.True(state3.Junctions.ContainsKey(junctionId));
        var j1 = state1.Junctions[junctionId];
        var j2 = state2.Junctions[junctionId];
        var j3 = state3.Junctions[junctionId];
        Assert.Equal(j1.JunctionId, j2.JunctionId);
        Assert.Equal(j1.JunctionId, j3.JunctionId);
        Assert.Equal(j1.BoundaryIds, j2.BoundaryIds);
        Assert.Equal(j1.BoundaryIds, j3.BoundaryIds);
        Assert.Equal(j1.Location, j2.Location);
        Assert.Equal(j1.Location, j3.Location);
        Assert.Equal(j1.IsRetired, j2.IsRetired);
        Assert.Equal(j1.IsRetired, j3.IsRetired);
    }
}
