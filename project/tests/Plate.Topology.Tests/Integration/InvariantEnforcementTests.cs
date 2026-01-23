using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;

namespace Plate.Topology.Tests.Integration;

/// <summary>
/// Integration tests for invariant validation during materialization per FR-010, FR-013, FR-016.
///
/// Tests verify that:
/// - BoundarySeparatesTwoPlates: boundaries reference existing non-retired plates
/// - NoOrphanJunctions: junctions only reference existing non-retired boundaries
/// - LifecycleOrdering: no mutations after retirement
/// - ReferenceValidity: events can't reference missing entities
/// - FR-016: boundary retirement requires explicit junction resolution
///
/// Per T0503: Validate invariants are enforced correctly with clear error messages.
/// </summary>
public class InvariantEnforcementTests : IDisposable
{
    private readonly PlateTopologyEventStore _store;
    private readonly TruthStreamIdentity _stream;
    private readonly PlateTopologyMaterializer _materializer;

    public InvariantEnforcementTests()
    {
        _store = TestStores.CreateEventStore();
        _stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0"
        );
        _materializer = new PlateTopologyMaterializer(_store);
    }

    public void Dispose()
    {
        _store.Dispose();
    }

    #region BoundarySeparatesTwoPlates Tests

    [Fact]
    public async Task BoundaryCreated_WithNonExistentLeftPlate_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateIdRight = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateIdRight, new CanonicalTick(0), 0, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                new PlateId(Guid.NewGuid()), // Non-existent left plate
                plateIdRight,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(1),
                1,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("BoundarySeparatesTwoPlates", exception.Message);
        Assert.Contains(boundaryId.ToString(), exception.Message);
        Assert.Contains("non-existent left plate", exception.Message);
        Assert.Contains("Sequence 1", exception.Message);
    }

    [Fact]
    public async Task BoundaryCreated_WithNonExistentRightPlate_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateIdLeft = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateIdLeft, new CanonicalTick(0), 0, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateIdLeft,
                new PlateId(Guid.NewGuid()), // Non-existent right plate
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(1),
                1,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("BoundarySeparatesTwoPlates", exception.Message);
        Assert.Contains(boundaryId.ToString(), exception.Message);
        Assert.Contains("non-existent right plate", exception.Message);
        Assert.Contains("Sequence 1", exception.Message);
    }

    [Fact]
    public async Task BoundaryCreated_WithRetiredLeftPlate_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateIdLeft = new PlateId(Guid.NewGuid());
        var plateIdRight = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateIdLeft, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateIdRight, new CanonicalTick(1), 1, _stream),
            new PlateRetiredEvent(Guid.NewGuid(), plateIdLeft, "test retirement", new CanonicalTick(2), 2, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateIdLeft, // Retired left plate
                plateIdRight,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("BoundarySeparatesTwoPlates", exception.Message);
        Assert.Contains(boundaryId.ToString(), exception.Message);
        Assert.Contains("retired left plate", exception.Message);
        Assert.Contains("Sequence 3", exception.Message);
    }

    [Fact]
    public async Task BoundaryCreated_WithSameLeftAndRightPlate_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateId = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId, new CanonicalTick(0), 0, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateId,
                plateId,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(1),
                1,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("BoundarySeparatesTwoPlates", exception.Message);
        Assert.Contains(boundaryId.ToString(), exception.Message);
        Assert.Contains("identical left and right plate", exception.Message);
        Assert.Contains("Sequence 1", exception.Message);
    }

    #endregion

    #region NoOrphanJunctions Tests

    [Fact]
    public async Task JunctionCreated_WithNonExistentBoundary_ThrowsInvalidOperationException()
    {
        // Arrange
        var junctionId = new JunctionId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new JunctionCreatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId], // Non-existent boundary
                new Point2D(0.0, 0.0),
                new CanonicalTick(0),
                0,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("NoOrphanJunctions", exception.Message);
        Assert.Contains(junctionId.ToString(), exception.Message);
        Assert.Contains("non-existent boundary", exception.Message);
        Assert.Contains(boundaryId.ToString(), exception.Message);
        Assert.Contains("Sequence 0", exception.Message);
    }

    [Fact]
    public async Task JunctionCreated_WithRetiredBoundary_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateIdLeft = new PlateId(Guid.NewGuid());
        var plateIdRight = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateIdLeft, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateIdRight, new CanonicalTick(1), 1, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateIdLeft,
                plateIdRight,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            new BoundaryRetiredEvent(Guid.NewGuid(), boundaryId, "test retirement", new CanonicalTick(3), 3, _stream),
            new JunctionCreatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId], // Retired boundary
                new Point2D(0.0, 0.0),
                new CanonicalTick(4),
                4,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("NoOrphanJunctions", exception.Message);
        Assert.Contains(junctionId.ToString(), exception.Message);
        Assert.Contains("retired boundary", exception.Message);
        Assert.Contains("Sequence 4", exception.Message);
    }

    [Fact]
    public async Task JunctionUpdated_WithNonExistentBoundary_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateIdLeft = new PlateId(Guid.NewGuid());
        var plateIdRight = new PlateId(Guid.NewGuid());
        var plateIdNew = new PlateId(Guid.NewGuid());
        var boundaryId1 = new BoundaryId(Guid.NewGuid());
        var boundaryId2 = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateIdLeft, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateIdRight, new CanonicalTick(1), 1, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateIdNew, new CanonicalTick(2), 2, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId1,
                plateIdLeft,
                plateIdRight,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),
            new JunctionCreatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId1],
                new Point2D(0.5, 0.0),
                new CanonicalTick(4),
                4,
                _stream
            ),
            new JunctionUpdatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId2], // Non-existent boundary
                null,
                new CanonicalTick(5),
                5,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("NoOrphanJunctions", exception.Message);
        Assert.Contains(junctionId.ToString(), exception.Message);
        Assert.Contains("non-existent boundary", exception.Message);
        Assert.Contains("Sequence 5", exception.Message);
    }

    #endregion

    #region LifecycleOrdering Tests

    [Fact]
    public async Task BoundaryTypeChanged_AfterRetirement_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            new BoundaryRetiredEvent(Guid.NewGuid(), boundaryId, "test retirement", new CanonicalTick(3), 3, _stream),
            new BoundaryTypeChangedEvent(
                Guid.NewGuid(),
                boundaryId,
                BoundaryType.Transform,
                BoundaryType.Convergent,
                new CanonicalTick(4),
                4,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("LifecycleOrdering", exception.Message);
        Assert.Contains("Cannot change type of retired boundary", exception.Message);
        Assert.Contains(boundaryId.ToString(), exception.Message);
        Assert.Contains("no mutation after retirement", exception.Message);
        Assert.Contains("Sequence 4", exception.Message);
    }

    [Fact]
    public async Task BoundaryGeometryUpdated_AfterRetirement_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            new BoundaryRetiredEvent(Guid.NewGuid(), boundaryId, "test retirement", new CanonicalTick(3), 3, _stream),
            new BoundaryGeometryUpdatedEvent(
                Guid.NewGuid(),
                boundaryId,
                new LineSegment(0.0, 0.0, 2.0, 0.0),
                new CanonicalTick(4),
                4,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("LifecycleOrdering", exception.Message);
        Assert.Contains("Cannot update geometry of retired boundary", exception.Message);
        Assert.Contains(boundaryId.ToString(), exception.Message);
        Assert.Contains("no mutation after retirement", exception.Message);
        Assert.Contains("Sequence 4", exception.Message);
    }

    [Fact]
    public async Task JunctionUpdated_AfterRetirement_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            new JunctionCreatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId],
                new Point2D(0.5, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),
            new JunctionRetiredEvent(Guid.NewGuid(), junctionId, "test retirement", new CanonicalTick(4), 4, _stream),
            new JunctionUpdatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId],
                new Point2D(0.6, 0.0),
                new CanonicalTick(5),
                5,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("LifecycleOrdering", exception.Message);
        Assert.Contains("Cannot update retired junction", exception.Message);
        Assert.Contains(junctionId.ToString(), exception.Message);
        Assert.Contains("no mutation after retirement", exception.Message);
        Assert.Contains("Sequence 5", exception.Message);
    }

    [Fact]
    public async Task PlateRetired_Twice_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateId = new PlateId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId, new CanonicalTick(0), 0, _stream),
            new PlateRetiredEvent(Guid.NewGuid(), plateId, "first retirement", new CanonicalTick(1), 1, _stream),
            new PlateRetiredEvent(Guid.NewGuid(), plateId, "second retirement", new CanonicalTick(2), 2, _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("LifecycleOrdering", exception.Message);
        Assert.Contains("already retired", exception.Message);
        Assert.Contains(plateId.ToString(), exception.Message);
        Assert.Contains("Sequence 2", exception.Message);
    }

    [Fact]
    public async Task BoundaryRetired_Twice_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            new BoundaryRetiredEvent(Guid.NewGuid(), boundaryId, "first retirement", new CanonicalTick(3), 3, _stream),
            new BoundaryRetiredEvent(Guid.NewGuid(), boundaryId, "second retirement", new CanonicalTick(4), 4, _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("LifecycleOrdering", exception.Message);
        Assert.Contains("already retired", exception.Message);
        Assert.Contains(boundaryId.ToString(), exception.Message);
        Assert.Contains("Sequence 4", exception.Message);
    }

    [Fact]
    public async Task JunctionRetired_Twice_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            new JunctionCreatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId],
                new Point2D(0.5, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),
            new JunctionRetiredEvent(Guid.NewGuid(), junctionId, "first retirement", new CanonicalTick(4), 4, _stream),
            new JunctionRetiredEvent(Guid.NewGuid(), junctionId, "second retirement", new CanonicalTick(5), 5, _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("LifecycleOrdering", exception.Message);
        Assert.Contains("already retired", exception.Message);
        Assert.Contains(junctionId.ToString(), exception.Message);
        Assert.Contains("Sequence 5", exception.Message);
    }

    #endregion

    #region ReferenceValidity Tests

    [Fact]
    public async Task BoundaryTypeChanged_NonExistentBoundary_ThrowsInvalidOperationException()
    {
        // Arrange
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new BoundaryTypeChangedEvent(
                Guid.NewGuid(),
                boundaryId,
                BoundaryType.Transform,
                BoundaryType.Convergent,
                new CanonicalTick(0),
                0,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("ReferenceValidity", exception.Message);
        Assert.Contains("non-existent boundary", exception.Message);
        Assert.Contains(boundaryId.ToString(), exception.Message);
        Assert.Contains("Sequence 0", exception.Message);
    }

    [Fact]
    public async Task BoundaryGeometryUpdated_NonExistentBoundary_ThrowsInvalidOperationException()
    {
        // Arrange
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new BoundaryGeometryUpdatedEvent(
                Guid.NewGuid(),
                boundaryId,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(0),
                0,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("ReferenceValidity", exception.Message);
        Assert.Contains("non-existent boundary", exception.Message);
        Assert.Contains(boundaryId.ToString(), exception.Message);
        Assert.Contains("Sequence 0", exception.Message);
    }

    [Fact]
    public async Task JunctionUpdated_NonExistentJunction_ThrowsInvalidOperationException()
    {
        // Arrange
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new JunctionUpdatedEvent(
                Guid.NewGuid(),
                junctionId,
                Array.Empty<BoundaryId>(),
                new Point2D(0.0, 0.0),
                new CanonicalTick(0),
                0,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("ReferenceValidity", exception.Message);
        Assert.Contains("non-existent junction", exception.Message);
        Assert.Contains(junctionId.ToString(), exception.Message);
        Assert.Contains("Sequence 0", exception.Message);
    }

    [Fact]
    public async Task PlateRetired_NonExistentPlate_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateId = new PlateId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateRetiredEvent(
                Guid.NewGuid(),
                plateId,
                "test retirement",
                new CanonicalTick(0),
                0,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("ReferenceValidity", exception.Message);
        Assert.Contains("non-existent plate", exception.Message);
        Assert.Contains(plateId.ToString(), exception.Message);
        Assert.Contains("Sequence 0", exception.Message);
    }

    #endregion

    #region FR-016 Boundary Deletion Tests

    [Fact]
    public async Task BoundaryRetired_WithActiveJunctionReferencing_ThrowsInvalidOperationException()
    {
        // Arrange
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            new JunctionCreatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId],
                new Point2D(0.5, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),
            // Attempt to retire boundary while junction still references it (FR-016 violation)
            new BoundaryRetiredEvent(
                Guid.NewGuid(),
                boundaryId,
                "test retirement",
                new CanonicalTick(4),
                4,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _materializer.MaterializeAsync(_stream, CancellationToken.None)
        );

        Assert.Contains("FR-016 BoundaryDeletion", exception.Message);
        Assert.Contains("Cannot retire boundary", exception.Message);
        Assert.Contains(boundaryId.ToString(), exception.Message);
        Assert.Contains("1 active junction(s) reference it", exception.Message);
        Assert.Contains(junctionId.ToString(), exception.Message);
        Assert.Contains("Must update or retire these junctions", exception.Message);
        Assert.Contains("explicit JunctionUpdated/JunctionRetired events", exception.Message);
        Assert.Contains("Sequence 4", exception.Message);
    }

    [Fact]
    public async Task BoundaryRetired_AfterJunctionRetired_SuccessfulMaterialization()
    {
        // Arrange
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                _stream
            ),
            new JunctionCreatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId],
                new Point2D(0.5, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),
            // First retire the junction (resolves the FR-016 constraint)
            new JunctionRetiredEvent(
                Guid.NewGuid(),
                junctionId,
                "test retirement",
                new CanonicalTick(4),
                4,
                _stream
            ),
            // Then retire the boundary (now valid)
            new BoundaryRetiredEvent(
                Guid.NewGuid(),
                boundaryId,
                "test retirement",
                new CanonicalTick(5),
                5,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act - Materialization should succeed
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);

        // Assert
        Assert.NotNull(state);
        Assert.Equal(2, state.Plates.Count); // Both plates exist
        Assert.Single(state.Boundaries); // Boundary exists (retired)
        Assert.Single(state.Junctions); // Junction exists (retired)

        var boundary = state.Boundaries[boundaryId];
        Assert.True(boundary.IsRetired);
        Assert.Equal("test retirement", boundary.RetirementReason);

        var junction = state.Junctions[junctionId];
        Assert.True(junction.IsRetired);
        Assert.Equal("test retirement", junction.RetirementReason);
    }

    [Fact]
    public async Task BoundaryRetired_AfterJunctionUpdated_SuccessfulMaterialization()
    {
        // Arrange
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var plateId3 = new PlateId(Guid.NewGuid());
        var boundaryId1 = new BoundaryId(Guid.NewGuid());
        var boundaryId2 = new BoundaryId(Guid.NewGuid());
        var junctionId = new JunctionId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId3, new CanonicalTick(2), 2, _stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId1,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(3),
                3,
                _stream
            ),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId2,
                plateId2,
                plateId3,
                BoundaryType.Transform,
                new LineSegment(1.0, 0.0, 2.0, 0.0),
                new CanonicalTick(4),
                4,
                _stream
            ),
            new JunctionCreatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId1, boundaryId2],
                new Point2D(1.0, 0.0),
                new CanonicalTick(5),
                5,
                _stream
            ),
            // Update junction to remove reference to boundaryId1 (resolves the FR-016 constraint)
            new JunctionUpdatedEvent(
                Guid.NewGuid(),
                junctionId,
                [boundaryId2], // Remove boundaryId1
                null,
                new CanonicalTick(6),
                6,
                _stream
            ),
            // Now can retire boundaryId1 (junction no longer references it)
            new BoundaryRetiredEvent(
                Guid.NewGuid(),
                boundaryId1,
                "test retirement",
                new CanonicalTick(7),
                7,
                _stream
            )
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act - Materialization should succeed
        var state = await _materializer.MaterializeAsync(_stream, CancellationToken.None);

        // Assert
        Assert.NotNull(state);
        Assert.Equal(3, state.Plates.Count);
        Assert.Equal(2, state.Boundaries.Count);
        Assert.Single(state.Junctions);

        var boundary1 = state.Boundaries[boundaryId1];
        Assert.True(boundary1.IsRetired);

        var boundary2 = state.Boundaries[boundaryId2];
        Assert.False(boundary2.IsRetired);

        var junction = state.Junctions[junctionId];
        Assert.False(junction.IsRetired);
        Assert.Single(junction.BoundaryIds);
        Assert.Equal(boundaryId2, junction.BoundaryIds[0]);
    }

    #endregion
}
