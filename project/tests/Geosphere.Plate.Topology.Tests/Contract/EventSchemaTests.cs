using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using Xunit;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Contract;

/// <summary>
/// Unit tests for plate topology event schemas per FR-008.
///
/// Tests verify that:
/// - All core event types implement IPlateTopologyEvent
/// - Required fields are non-null/valid
/// - EventType returns the correct discriminator for polymorphic deserialization
/// - Events follow immutable record struct pattern
/// </summary>
public class EventSchemaTests
{
    #region Test Helpers

    /// <summary>
    /// Creates a valid truth stream identity for testing.
    /// </summary>
    private static TruthStreamIdentity CreateValidStreamIdentity()
    {
        return new TruthStreamIdentity(
            "test-variant",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0"
        );
    }

    /// <summary>
    /// Creates a valid event tick for testing (per RFC-V2-0010).
    /// </summary>
    private static CanonicalTick CreateValidTick()
    {
        return new CanonicalTick(0);
    }

    #endregion

    #region PlateCreatedEvent Tests

    [Fact]
    public void PlateCreatedEvent_ImplementsIPlateTopologyEvent()
    {
        // Arrange
        var @event = TestEventFactory.PlateCreated(
            Guid.NewGuid(),
            PlateId.NewId(),
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.IsAssignableFrom<IPlateTopologyEvent>(@event);
    }

    [Fact]
    public void PlateCreatedEvent_RequiredFieldsAreValid()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var plateId = PlateId.NewId();
        var tick = CreateValidTick();
        var sequence = 0L;
        var streamIdentity = CreateValidStreamIdentity();

        // Act
        var @event = TestEventFactory.PlateCreated(eventId, plateId, tick, sequence, streamIdentity);

        // Assert - All required fields should be valid
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.False(@event.PlateId.IsEmpty);
        Assert.True(@event.Tick.Value >= 0);
        Assert.True(@event.StreamIdentity.IsValid());
    }

    [Fact]
    public void PlateCreatedEvent_EventType_ReturnsCorrectDiscriminator()
    {
        // Arrange & Act
        var @event = TestEventFactory.PlateCreated(
            Guid.NewGuid(),
            PlateId.NewId(),
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - EventType is explicitly implemented, cast to interface
        Assert.Equal("PlateCreatedEvent", ((IPlateTopologyEvent)@event).EventType);
    }

    [Fact]
    public void PlateCreatedEvent_FieldsAreAccessible()
    {
        // Arrange
        var eventId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var plateId = PlateId.Parse("87654321-4321-4321-4321-cba987654321");
        var tick = new CanonicalTick(100);
        var sequence = 5L;
        var streamIdentity = CreateValidStreamIdentity();

        // Act
        var @event = TestEventFactory.PlateCreated(eventId, plateId, tick, sequence, streamIdentity);

        // Assert
        Assert.Equal(eventId, @event.EventId);
        Assert.Equal(plateId, @event.PlateId);
        Assert.Equal(tick, @event.Tick);
        Assert.Equal(sequence, @event.Sequence);
        Assert.Equal(streamIdentity, @event.StreamIdentity);
    }

    #endregion

    #region BoundaryCreatedEvent Tests

    [Fact]
    public void BoundaryCreatedEvent_ImplementsIPlateTopologyEvent()
    {
        // Arrange
        var @event = TestEventFactory.BoundaryCreated(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            PlateId.NewId(),
            PlateId.NewId(),
            BoundaryType.Divergent,
            new Segment2(0, 0, 1, 1),
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.IsAssignableFrom<IPlateTopologyEvent>(@event);
    }

    [Fact]
    public void BoundaryCreatedEvent_RequiredFieldsAreValid()
    {
        // Arrange
        var boundaryId = BoundaryId.NewId();
        var plateIdLeft = PlateId.NewId();
        var plateIdRight = PlateId.NewId();
        var boundaryType = BoundaryType.Convergent;
        var geometry = Polyline2.FromCoordinates(0, 0, 1, 1, 2, 0);

        // Act
        var @event = TestEventFactory.BoundaryCreated(
            Guid.NewGuid(),
            boundaryId,
            plateIdLeft,
            plateIdRight,
            boundaryType,
            geometry,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.False(@event.BoundaryId.IsEmpty);
        Assert.False(@event.PlateIdLeft.IsEmpty);
        Assert.False(@event.PlateIdRight.IsEmpty);
        Assert.NotNull(@event.Geometry);
        Assert.True(@event.StreamIdentity.IsValid());
    }

    [Fact]
    public void BoundaryCreatedEvent_EventType_ReturnsCorrectDiscriminator()
    {
        // Arrange & Act
        var @event = TestEventFactory.BoundaryCreated(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            PlateId.NewId(),
            PlateId.NewId(),
            BoundaryType.Transform,
            new Segment2(0, 0, 1, 1),
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - EventType is explicitly implemented, cast to interface
        Assert.Equal("BoundaryCreatedEvent", ((IPlateTopologyEvent)@event).EventType);
    }

    [Fact]
    public void BoundaryCreatedEvent_SupportsAllBoundaryTypes()
    {
        // Arrange
        var boundaryTypes = new[] { BoundaryType.Divergent, BoundaryType.Convergent, BoundaryType.Transform };

        // Act & Assert
        foreach (var boundaryType in boundaryTypes)
        {
            var @event = TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                BoundaryId.NewId(),
                PlateId.NewId(),
                PlateId.NewId(),
                boundaryType,
                new Segment2(0, 0, 1, 1),
                new CanonicalTick(0),
                0L,
                CreateValidStreamIdentity()
            );

            Assert.Equal(boundaryType, @event.BoundaryType);
        }
    }

    [Fact]
    public void BoundaryCreatedEvent_Geometry_IsAccessible()
    {
        // Arrange
        var geometry = new Segment2(0, 0, 10, 10);

        // Act
        var @event = TestEventFactory.BoundaryCreated(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            PlateId.NewId(),
            PlateId.NewId(),
            BoundaryType.Divergent,
            geometry,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - Cast to concrete type to access specific properties
        var lineSegment = (Segment2)@event.Geometry;
        Assert.Equal(geometry, @event.Geometry);
        Assert.Equal(0.0, lineSegment.Start.X);
        Assert.Equal(0.0, lineSegment.Start.Y);
    }

    #endregion

    #region JunctionCreatedEvent Tests

    [Fact]
    public void JunctionCreatedEvent_ImplementsIPlateTopologyEvent()
    {
        // Arrange
        var @event = TestEventFactory.JunctionCreated(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId(), BoundaryId.NewId(), BoundaryId.NewId() },
            new Point2(5, 5),
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.IsAssignableFrom<IPlateTopologyEvent>(@event);
    }

    [Fact]
    public void JunctionCreatedEvent_RequiredFieldsAreValid()
    {
        // Arrange
        var junctionId = JunctionId.NewId();
        var boundaryIds = new[] { BoundaryId.NewId(), BoundaryId.NewId() };
        var location = new Point2(10, 20);

        // Act
        var @event = TestEventFactory.JunctionCreated(
            Guid.NewGuid(),
            junctionId,
            boundaryIds,
            location,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.False(@event.JunctionId.IsEmpty);
        Assert.NotNull(@event.BoundaryIds);
        Assert.NotEmpty(@event.BoundaryIds);
        Assert.False(@event.Location.IsEmpty);
        Assert.True(@event.StreamIdentity.IsValid());
    }

    [Fact]
    public void JunctionCreatedEvent_EventType_ReturnsCorrectDiscriminator()
    {
        // Arrange & Act
        var @event = TestEventFactory.JunctionCreated(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId(), BoundaryId.NewId() },
            new Point2(0, 0),
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - EventType is explicitly implemented, cast to interface
        Assert.Equal("JunctionCreatedEvent", ((IPlateTopologyEvent)@event).EventType);
    }

    [Fact]
    public void JunctionCreatedEvent_BoundaryIds_AreAccessible()
    {
        // Arrange
        var boundaryIds = new[]
        {
            BoundaryId.Parse("11111111-1111-1111-1111-111111111111"),
            BoundaryId.Parse("22222222-2222-2222-2222-222222222222"),
            BoundaryId.Parse("33333333-3333-3333-3333-333333333333")
        };

        // Act
        var @event = TestEventFactory.JunctionCreated(
            Guid.NewGuid(),
            JunctionId.NewId(),
            boundaryIds,
            new Point2(0, 0),
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.Equal(3, @event.BoundaryIds.Length);
        Assert.Equal(boundaryIds[0], @event.BoundaryIds[0]);
        Assert.Equal(boundaryIds[1], @event.BoundaryIds[1]);
        Assert.Equal(boundaryIds[2], @event.BoundaryIds[2]);
    }

    [Fact]
    public void JunctionCreatedEvent_Location_IsAccessible()
    {
        // Arrange
        var location = new Point2(42.5, -17.3);

        // Act
        var @event = TestEventFactory.JunctionCreated(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId() },
            location,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.Equal(42.5, @event.Location.X);
        Assert.Equal(-17.3, @event.Location.Y);
    }

    #endregion

    #region BoundaryTypeChangedEvent Tests

    [Fact]
    public void BoundaryTypeChangedEvent_ImplementsIPlateTopologyEvent()
    {
        // Arrange
        var @event = TestEventFactory.BoundaryTypeChanged(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            BoundaryType.Divergent,
            BoundaryType.Convergent,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.IsAssignableFrom<IPlateTopologyEvent>(@event);
    }

    [Fact]
    public void BoundaryTypeChangedEvent_RequiredFieldsAreValid()
    {
        // Arrange
        var boundaryId = BoundaryId.NewId();
        var oldType = BoundaryType.Divergent;
        var newType = BoundaryType.Convergent;

        // Act
        var @event = TestEventFactory.BoundaryTypeChanged(
            Guid.NewGuid(),
            boundaryId,
            oldType,
            newType,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.False(@event.BoundaryId.IsEmpty);
        Assert.True(@event.StreamIdentity.IsValid());
    }

    [Fact]
    public void BoundaryTypeChangedEvent_EventType_ReturnsCorrectDiscriminator()
    {
        // Arrange & Act
        var @event = TestEventFactory.BoundaryTypeChanged(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            BoundaryType.Convergent,
            BoundaryType.Transform,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - EventType is explicitly implemented, cast to interface
        Assert.Equal("BoundaryTypeChangedEvent", ((IPlateTopologyEvent)@event).EventType);
    }

    [Fact]
    public void BoundaryTypeChangedEvent_TypeTransition_IsAccessible()
    {
        // Arrange
        var oldType = BoundaryType.Divergent;
        var newType = BoundaryType.Convergent;

        // Act
        var @event = TestEventFactory.BoundaryTypeChanged(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            oldType,
            newType,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.Equal(oldType, @event.OldType);
        Assert.Equal(newType, @event.NewType);
        Assert.NotEqual(@event.OldType, @event.NewType);
    }

    #endregion

    #region BoundaryGeometryUpdatedEvent Tests

    [Fact]
    public void BoundaryGeometryUpdatedEvent_ImplementsIPlateTopologyEvent()
    {
        // Arrange
        var @event = TestEventFactory.BoundaryGeometryUpdated(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            Polyline2.FromCoordinates(0, 0, 1, 1, 2, 0),
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.IsAssignableFrom<IPlateTopologyEvent>(@event);
    }

    [Fact]
    public void BoundaryGeometryUpdatedEvent_RequiredFieldsAreValid()
    {
        // Arrange
        var boundaryId = BoundaryId.NewId();
        var newGeometry = new Segment2(0, 0, 100, 100);

        // Act
        var @event = TestEventFactory.BoundaryGeometryUpdated(
            Guid.NewGuid(),
            boundaryId,
            newGeometry,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.False(@event.BoundaryId.IsEmpty);
        Assert.NotNull(@event.NewGeometry);
        Assert.True(@event.StreamIdentity.IsValid());
    }

    [Fact]
    public void BoundaryGeometryUpdatedEvent_EventType_ReturnsCorrectDiscriminator()
    {
        // Arrange & Act
        var @event = TestEventFactory.BoundaryGeometryUpdated(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            new Segment2(0, 0, 1, 1),
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - EventType is explicitly implemented, cast to interface
        Assert.Equal("BoundaryGeometryUpdatedEvent", ((IPlateTopologyEvent)@event).EventType);
    }

    [Fact]
    public void BoundaryGeometryUpdatedEvent_NewGeometry_IsAccessible()
    {
        // Arrange
        var newGeometry = Polyline2.FromCoordinates(0, 0, 10, 10, 20, 0);

        // Act
        var @event = TestEventFactory.BoundaryGeometryUpdated(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            newGeometry,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - Cast to concrete type to access specific properties
        Assert.Equal(newGeometry, @event.NewGeometry);
        Assert.Equal(3, ((Polyline2)@event.NewGeometry).PointCount);
    }

    #endregion

    #region BoundaryRetiredEvent Tests

    [Fact]
    public void BoundaryRetiredEvent_ImplementsIPlateTopologyEvent()
    {
        // Arrange
        var @event = TestEventFactory.BoundaryRetired(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            "Plate merger",
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.IsAssignableFrom<IPlateTopologyEvent>(@event);
    }

    [Fact]
    public void BoundaryRetiredEvent_RequiredFieldsAreValid()
    {
        // Arrange
        var boundaryId = BoundaryId.NewId();

        // Act
        var @event = TestEventFactory.BoundaryRetired(
            Guid.NewGuid(),
            boundaryId,
            "Plate merger",
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.False(@event.BoundaryId.IsEmpty);
        Assert.True(@event.StreamIdentity.IsValid());
    }

    [Fact]
    public void BoundaryRetiredEvent_EventType_ReturnsCorrectDiscriminator()
    {
        // Arrange & Act
        var @event = TestEventFactory.BoundaryRetired(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            "Plate merger",
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - EventType is explicitly implemented, cast to interface
        Assert.Equal("BoundaryRetiredEvent", ((IPlateTopologyEvent)@event).EventType);
    }

    [Fact]
    public void BoundaryRetiredEvent_Reason_CanBeNull()
    {
        // Arrange & Act
        var @event = TestEventFactory.BoundaryRetired(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            null,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.Null(@event.Reason);
    }

    [Fact]
    public void BoundaryRetiredEvent_Reason_IsAccessible()
    {
        // Arrange
        var reason = "Boundary removed due to plate merger event";

        // Act
        var @event = TestEventFactory.BoundaryRetired(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            reason,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.Equal(reason, @event.Reason);
    }

    #endregion

    #region JunctionUpdatedEvent Tests

    [Fact]
    public void JunctionUpdatedEvent_ImplementsIPlateTopologyEvent()
    {
        // Arrange
        var @event = TestEventFactory.JunctionUpdated(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId(), BoundaryId.NewId() },
            new Point2(10, 20),
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.IsAssignableFrom<IPlateTopologyEvent>(@event);
    }

    [Fact]
    public void JunctionUpdatedEvent_RequiredFieldsAreValid()
    {
        // Arrange
        var junctionId = JunctionId.NewId();
        var newBoundaryIds = new[] { BoundaryId.NewId(), BoundaryId.NewId() };

        // Act
        var @event = TestEventFactory.JunctionUpdated(
            Guid.NewGuid(),
            junctionId,
            newBoundaryIds,
            new Point2(15, 25),
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.False(@event.JunctionId.IsEmpty);
        Assert.NotNull(@event.NewBoundaryIds);
        Assert.NotEmpty(@event.NewBoundaryIds);
        Assert.True(@event.StreamIdentity.IsValid());
    }

    [Fact]
    public void JunctionUpdatedEvent_EventType_ReturnsCorrectDiscriminator()
    {
        // Arrange & Act
        var @event = TestEventFactory.JunctionUpdated(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId() },
            null,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - EventType is explicitly implemented, cast to interface
        Assert.Equal("JunctionUpdatedEvent", ((IPlateTopologyEvent)@event).EventType);
    }

    [Fact]
    public void JunctionUpdatedEvent_NewBoundaryIds_AreAccessible()
    {
        // Arrange
        var newBoundaryIds = new[]
        {
            BoundaryId.NewId(),
            BoundaryId.NewId(),
            BoundaryId.NewId()
        };

        // Act
        var @event = TestEventFactory.JunctionUpdated(
            Guid.NewGuid(),
            JunctionId.NewId(),
            newBoundaryIds,
            null,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.Equal(3, @event.NewBoundaryIds.Length);
    }

    [Fact]
    public void JunctionUpdatedEvent_NewLocation_CanBeNull()
    {
        // Arrange & Act
        var @event = TestEventFactory.JunctionUpdated(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId() },
            null,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.Null(@event.NewLocation);
    }

    [Fact]
    public void JunctionUpdatedEvent_NewLocation_IsAccessible()
    {
        // Arrange
        var newLocation = new Point2(100, 200);

        // Act
        var @event = TestEventFactory.JunctionUpdated(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId() },
            newLocation,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.Equal(newLocation, @event.NewLocation);
        Assert.Equal(100, @event.NewLocation.Value.X);
        Assert.Equal(200, @event.NewLocation.Value.Y);
    }

    #endregion

    #region JunctionRetiredEvent Tests

    [Fact]
    public void JunctionRetiredEvent_ImplementsIPlateTopologyEvent()
    {
        // Arrange
        var @event = TestEventFactory.JunctionRetired(
            Guid.NewGuid(),
            JunctionId.NewId(),
            "Junction merged into another",
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.IsAssignableFrom<IPlateTopologyEvent>(@event);
    }

    [Fact]
    public void JunctionRetiredEvent_RequiredFieldsAreValid()
    {
        // Arrange
        var junctionId = JunctionId.NewId();

        // Act
        var @event = TestEventFactory.JunctionRetired(
            Guid.NewGuid(),
            junctionId,
            "Junction retired",
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.False(@event.JunctionId.IsEmpty);
        Assert.True(@event.StreamIdentity.IsValid());
    }

    [Fact]
    public void JunctionRetiredEvent_EventType_ReturnsCorrectDiscriminator()
    {
        // Arrange & Act
        var @event = TestEventFactory.JunctionRetired(
            Guid.NewGuid(),
            JunctionId.NewId(),
            "Junction removed",
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - EventType is explicitly implemented, cast to interface
        Assert.Equal("JunctionRetiredEvent", ((IPlateTopologyEvent)@event).EventType);
    }

    [Fact]
    public void JunctionRetiredEvent_Reason_CanBeNull()
    {
        // Arrange & Act
        var @event = TestEventFactory.JunctionRetired(
            Guid.NewGuid(),
            JunctionId.NewId(),
            null,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.Null(@event.Reason);
    }

    [Fact]
    public void JunctionRetiredEvent_Reason_IsAccessible()
    {
        // Arrange
        var reason = "Junction removed due to boundary reorganization";

        // Act
        var @event = TestEventFactory.JunctionRetired(
            Guid.NewGuid(),
            JunctionId.NewId(),
            reason,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.Equal(reason, @event.Reason);
    }

    #endregion

    #region PlateRetiredEvent Tests

    [Fact]
    public void PlateRetiredEvent_ImplementsIPlateTopologyEvent()
    {
        // Arrange
        var @event = TestEventFactory.PlateRetired(
            Guid.NewGuid(),
            PlateId.NewId(),
            "Plate subducted completely",
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.IsAssignableFrom<IPlateTopologyEvent>(@event);
    }

    [Fact]
    public void PlateRetiredEvent_RequiredFieldsAreValid()
    {
        // Arrange
        var plateId = PlateId.NewId();

        // Act
        var @event = TestEventFactory.PlateRetired(
            Guid.NewGuid(),
            plateId,
            "Plate retired",
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.False(@event.PlateId.IsEmpty);
        Assert.True(@event.StreamIdentity.IsValid());
    }

    [Fact]
    public void PlateRetiredEvent_EventType_ReturnsCorrectDiscriminator()
    {
        // Arrange & Act
        var @event = TestEventFactory.PlateRetired(
            Guid.NewGuid(),
            PlateId.NewId(),
            "Plate removed",
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - EventType is explicitly implemented, cast to interface
        Assert.Equal("PlateRetiredEvent", ((IPlateTopologyEvent)@event).EventType);
    }

    [Fact]
    public void PlateRetiredEvent_Reason_CanBeNull()
    {
        // Arrange & Act
        var @event = TestEventFactory.PlateRetired(
            Guid.NewGuid(),
            PlateId.NewId(),
            null,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.Null(@event.Reason);
    }

    [Fact]
    public void PlateRetiredEvent_Reason_IsAccessible()
    {
        // Arrange
        var reason = "Plate completely subducted";

        // Act
        var @event = TestEventFactory.PlateRetired(
            Guid.NewGuid(),
            PlateId.NewId(),
            reason,
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert
        Assert.Equal(reason, @event.Reason);
    }

    #endregion

    #region Cross-Event Tests

    [Fact]
    public void AllEvents_ImplementIPlateTopologyEvent()
    {
        // Arrange - Create instances of all event types
        var streamIdentity = CreateValidStreamIdentity();
        var timestamp = new CanonicalTick(0);
        var sequence = 0L;
        var eventId = Guid.NewGuid();

        var events = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(eventId, PlateId.NewId(), timestamp, sequence, streamIdentity),
            TestEventFactory.BoundaryCreated(eventId, BoundaryId.NewId(), PlateId.NewId(), PlateId.NewId(),
                BoundaryType.Divergent, new Segment2(0, 0, 1, 1), timestamp, sequence, streamIdentity),
            TestEventFactory.JunctionCreated(eventId, JunctionId.NewId(),
                new[] { BoundaryId.NewId() }, new Point2(0, 0), timestamp, sequence, streamIdentity),
            TestEventFactory.BoundaryTypeChanged(eventId, BoundaryId.NewId(),
                BoundaryType.Divergent, BoundaryType.Convergent, timestamp, sequence, streamIdentity),
            TestEventFactory.BoundaryGeometryUpdated(eventId, BoundaryId.NewId(),
                new Segment2(0, 0, 1, 1), timestamp, sequence, streamIdentity),
            TestEventFactory.BoundaryRetired(eventId, BoundaryId.NewId(), "reason", timestamp, sequence, streamIdentity),
            TestEventFactory.JunctionUpdated(eventId, JunctionId.NewId(),
                new[] { BoundaryId.NewId() }, null, timestamp, sequence, streamIdentity),
            TestEventFactory.JunctionRetired(eventId, JunctionId.NewId(), "reason", timestamp, sequence, streamIdentity),
            TestEventFactory.PlateRetired(eventId, PlateId.NewId(), "reason", timestamp, sequence, streamIdentity)
        };

        // Assert - All events should implement IPlateTopologyEvent
        Assert.All(events, e => Assert.IsAssignableFrom<IPlateTopologyEvent>(e));
    }

    [Fact]
    public void AllEvents_EventType_ReturnsCorrectDiscriminator()
    {
        // Arrange
        var streamIdentity = CreateValidStreamIdentity();
        var timestamp = new CanonicalTick(0);
        var sequence = 0L;
        var eventId = Guid.NewGuid();

        var eventTypes = new (IPlateTopologyEvent Event, string ExpectedType)[]
        {
            (TestEventFactory.PlateCreated(eventId, PlateId.NewId(), timestamp, sequence, streamIdentity), "PlateCreatedEvent"),
            (TestEventFactory.BoundaryCreated(eventId, BoundaryId.NewId(), PlateId.NewId(), PlateId.NewId(),
                BoundaryType.Divergent, new Segment2(0, 0, 1, 1), timestamp, sequence, streamIdentity), "BoundaryCreatedEvent"),
            (TestEventFactory.JunctionCreated(eventId, JunctionId.NewId(),
                new[] { BoundaryId.NewId() }, new Point2(0, 0), timestamp, sequence, streamIdentity), "JunctionCreatedEvent"),
            (TestEventFactory.BoundaryTypeChanged(eventId, BoundaryId.NewId(),
                BoundaryType.Divergent, BoundaryType.Convergent, timestamp, sequence, streamIdentity), "BoundaryTypeChangedEvent"),
            (TestEventFactory.BoundaryGeometryUpdated(eventId, BoundaryId.NewId(),
                new Segment2(0, 0, 1, 1), timestamp, sequence, streamIdentity), "BoundaryGeometryUpdatedEvent"),
            (TestEventFactory.BoundaryRetired(eventId, BoundaryId.NewId(), "reason", timestamp, sequence, streamIdentity), "BoundaryRetiredEvent"),
            (TestEventFactory.JunctionUpdated(eventId, JunctionId.NewId(),
                new[] { BoundaryId.NewId() }, null, timestamp, sequence, streamIdentity), "JunctionUpdatedEvent"),
            (TestEventFactory.JunctionRetired(eventId, JunctionId.NewId(), "reason", timestamp, sequence, streamIdentity), "JunctionRetiredEvent"),
            (TestEventFactory.PlateRetired(eventId, PlateId.NewId(), "reason", timestamp, sequence, streamIdentity), "PlateRetiredEvent")
        };

        // Act & Assert - EventType is explicitly implemented, accessed via interface
        foreach (var (eventObj, expectedType) in eventTypes)
        {
            Assert.Equal(expectedType, eventObj.EventType);
        }
    }

    [Fact]
    public void AllEvents_AreImmutableRecordStructs()
    {
        // Arrange - Create an instance and check it's a record struct
        var @event = TestEventFactory.PlateCreated(
            Guid.NewGuid(),
            PlateId.NewId(),
            new CanonicalTick(0),
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - Verify the event type and struct behavior
        Assert.IsAssignableFrom<IPlateTopologyEvent>(@event);
        // Record structs implement IEquatable<T>
        Assert.IsAssignableFrom<IEquatable<PlateCreatedEvent>>(@event);
    }

    #endregion
}
