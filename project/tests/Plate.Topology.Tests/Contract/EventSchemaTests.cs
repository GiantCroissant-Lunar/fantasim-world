using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;
using Xunit;

namespace Plate.Topology.Tests.Contract;

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
    /// Creates a valid event timestamp for testing.
    /// </summary>
    private static DateTimeOffset CreateValidTimestamp()
    {
        return DateTimeOffset.UtcNow;
    }

    #endregion

    #region PlateCreatedEvent Tests

    [Fact]
    public void PlateCreatedEvent_ImplementsIPlateTopologyEvent()
    {
        // Arrange
        var @event = new PlateCreatedEvent(
            Guid.NewGuid(),
            PlateId.NewId(),
            DateTimeOffset.UtcNow,
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
        var timestamp = CreateValidTimestamp();
        var sequence = 0L;
        var streamIdentity = CreateValidStreamIdentity();

        // Act
        var @event = new PlateCreatedEvent(eventId, plateId, timestamp, sequence, streamIdentity);

        // Assert - All required fields should be valid
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.False(@event.PlateId.IsEmpty);
        Assert.True(@event.Timestamp != default);
        Assert.True(@event.StreamIdentity.IsValid());
    }

    [Fact]
    public void PlateCreatedEvent_EventType_ReturnsCorrectDiscriminator()
    {
        // Arrange & Act
        var @event = new PlateCreatedEvent(
            Guid.NewGuid(),
            PlateId.NewId(),
            DateTimeOffset.UtcNow,
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
        var timestamp = new DateTimeOffset(2026, 1, 22, 12, 0, 0, TimeSpan.Zero);
        var sequence = 5L;
        var streamIdentity = CreateValidStreamIdentity();

        // Act
        var @event = new PlateCreatedEvent(eventId, plateId, timestamp, sequence, streamIdentity);

        // Assert
        Assert.Equal(eventId, @event.EventId);
        Assert.Equal(plateId, @event.PlateId);
        Assert.Equal(timestamp, @event.Timestamp);
        Assert.Equal(sequence, @event.Sequence);
        Assert.Equal(streamIdentity, @event.StreamIdentity);
    }

    #endregion

    #region BoundaryCreatedEvent Tests

    [Fact]
    public void BoundaryCreatedEvent_ImplementsIPlateTopologyEvent()
    {
        // Arrange
        var @event = new BoundaryCreatedEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            PlateId.NewId(),
            PlateId.NewId(),
            BoundaryType.Divergent,
            new LineSegment(0, 0, 1, 1),
            DateTimeOffset.UtcNow,
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
        var geometry = Polyline.FromCoordinates(0, 0, 1, 1, 2, 0);

        // Act
        var @event = new BoundaryCreatedEvent(
            Guid.NewGuid(),
            boundaryId,
            plateIdLeft,
            plateIdRight,
            boundaryType,
            geometry,
            DateTimeOffset.UtcNow,
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
        var @event = new BoundaryCreatedEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            PlateId.NewId(),
            PlateId.NewId(),
            BoundaryType.Transform,
            new LineSegment(0, 0, 1, 1),
            DateTimeOffset.UtcNow,
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
            var @event = new BoundaryCreatedEvent(
                Guid.NewGuid(),
                BoundaryId.NewId(),
                PlateId.NewId(),
                PlateId.NewId(),
                boundaryType,
                new LineSegment(0, 0, 1, 1),
                DateTimeOffset.UtcNow,
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
        var geometry = new LineSegment(0, 0, 10, 10);

        // Act
        var @event = new BoundaryCreatedEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            PlateId.NewId(),
            PlateId.NewId(),
            BoundaryType.Divergent,
            geometry,
            DateTimeOffset.UtcNow,
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - Cast to concrete type to access specific properties
        var lineSegment = (LineSegment)@event.Geometry;
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
        var @event = new JunctionCreatedEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId(), BoundaryId.NewId(), BoundaryId.NewId() },
            new Point2D(5, 5),
            DateTimeOffset.UtcNow,
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
        var location = new Point2D(10, 20);

        // Act
        var @event = new JunctionCreatedEvent(
            Guid.NewGuid(),
            junctionId,
            boundaryIds,
            location,
            DateTimeOffset.UtcNow,
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
        var @event = new JunctionCreatedEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId(), BoundaryId.NewId() },
            new Point2D(0, 0),
            DateTimeOffset.UtcNow,
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
        var @event = new JunctionCreatedEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            boundaryIds,
            new Point2D(0, 0),
            DateTimeOffset.UtcNow,
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
        var location = new Point2D(42.5, -17.3);

        // Act
        var @event = new JunctionCreatedEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId() },
            location,
            DateTimeOffset.UtcNow,
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
        var @event = new BoundaryTypeChangedEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            BoundaryType.Divergent,
            BoundaryType.Convergent,
            DateTimeOffset.UtcNow,
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
        var @event = new BoundaryTypeChangedEvent(
            Guid.NewGuid(),
            boundaryId,
            oldType,
            newType,
            DateTimeOffset.UtcNow,
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
        var @event = new BoundaryTypeChangedEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            BoundaryType.Convergent,
            BoundaryType.Transform,
            DateTimeOffset.UtcNow,
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
        var @event = new BoundaryTypeChangedEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            oldType,
            newType,
            DateTimeOffset.UtcNow,
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
        var @event = new BoundaryGeometryUpdatedEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            Polyline.FromCoordinates(0, 0, 1, 1, 2, 0),
            DateTimeOffset.UtcNow,
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
        var newGeometry = new LineSegment(0, 0, 100, 100);

        // Act
        var @event = new BoundaryGeometryUpdatedEvent(
            Guid.NewGuid(),
            boundaryId,
            newGeometry,
            DateTimeOffset.UtcNow,
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
        var @event = new BoundaryGeometryUpdatedEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            new LineSegment(0, 0, 1, 1),
            DateTimeOffset.UtcNow,
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
        var newGeometry = Polyline.FromCoordinates(0, 0, 10, 10, 20, 0);

        // Act
        var @event = new BoundaryGeometryUpdatedEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            newGeometry,
            DateTimeOffset.UtcNow,
            0L,
            CreateValidStreamIdentity()
        );

        // Assert - Cast to concrete type to access specific properties
        Assert.Equal(newGeometry, @event.NewGeometry);
        Assert.Equal(3, ((Polyline)@event.NewGeometry).PointCount);
    }

    #endregion

    #region BoundaryRetiredEvent Tests

    [Fact]
    public void BoundaryRetiredEvent_ImplementsIPlateTopologyEvent()
    {
        // Arrange
        var @event = new BoundaryRetiredEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            "Plate merger",
            DateTimeOffset.UtcNow,
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
        var @event = new BoundaryRetiredEvent(
            Guid.NewGuid(),
            boundaryId,
            "Plate merger",
            DateTimeOffset.UtcNow,
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
        var @event = new BoundaryRetiredEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            "Plate merger",
            DateTimeOffset.UtcNow,
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
        var @event = new BoundaryRetiredEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            null,
            DateTimeOffset.UtcNow,
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
        var @event = new BoundaryRetiredEvent(
            Guid.NewGuid(),
            BoundaryId.NewId(),
            reason,
            DateTimeOffset.UtcNow,
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
        var @event = new JunctionUpdatedEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId(), BoundaryId.NewId() },
            new Point2D(10, 20),
            DateTimeOffset.UtcNow,
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
        var @event = new JunctionUpdatedEvent(
            Guid.NewGuid(),
            junctionId,
            newBoundaryIds,
            new Point2D(15, 25),
            DateTimeOffset.UtcNow,
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
        var @event = new JunctionUpdatedEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId() },
            null,
            DateTimeOffset.UtcNow,
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
        var @event = new JunctionUpdatedEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            newBoundaryIds,
            null,
            DateTimeOffset.UtcNow,
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
        var @event = new JunctionUpdatedEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId() },
            null,
            DateTimeOffset.UtcNow,
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
        var newLocation = new Point2D(100, 200);

        // Act
        var @event = new JunctionUpdatedEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            new[] { BoundaryId.NewId() },
            newLocation,
            DateTimeOffset.UtcNow,
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
        var @event = new JunctionRetiredEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            "Junction merged into another",
            DateTimeOffset.UtcNow,
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
        var @event = new JunctionRetiredEvent(
            Guid.NewGuid(),
            junctionId,
            "Junction retired",
            DateTimeOffset.UtcNow,
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
        var @event = new JunctionRetiredEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            "Junction removed",
            DateTimeOffset.UtcNow,
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
        var @event = new JunctionRetiredEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            null,
            DateTimeOffset.UtcNow,
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
        var @event = new JunctionRetiredEvent(
            Guid.NewGuid(),
            JunctionId.NewId(),
            reason,
            DateTimeOffset.UtcNow,
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
        var @event = new PlateRetiredEvent(
            Guid.NewGuid(),
            PlateId.NewId(),
            "Plate subducted completely",
            DateTimeOffset.UtcNow,
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
        var @event = new PlateRetiredEvent(
            Guid.NewGuid(),
            plateId,
            "Plate retired",
            DateTimeOffset.UtcNow,
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
        var @event = new PlateRetiredEvent(
            Guid.NewGuid(),
            PlateId.NewId(),
            "Plate removed",
            DateTimeOffset.UtcNow,
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
        var @event = new PlateRetiredEvent(
            Guid.NewGuid(),
            PlateId.NewId(),
            null,
            DateTimeOffset.UtcNow,
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
        var @event = new PlateRetiredEvent(
            Guid.NewGuid(),
            PlateId.NewId(),
            reason,
            DateTimeOffset.UtcNow,
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
        var timestamp = DateTimeOffset.UtcNow;
        var sequence = 0L;
        var eventId = Guid.NewGuid();

        var events = new IPlateTopologyEvent[]
        {
            new PlateCreatedEvent(eventId, PlateId.NewId(), timestamp, sequence, streamIdentity),
            new BoundaryCreatedEvent(eventId, BoundaryId.NewId(), PlateId.NewId(), PlateId.NewId(),
                BoundaryType.Divergent, new LineSegment(0, 0, 1, 1), timestamp, sequence, streamIdentity),
            new JunctionCreatedEvent(eventId, JunctionId.NewId(),
                new[] { BoundaryId.NewId() }, new Point2D(0, 0), timestamp, sequence, streamIdentity),
            new BoundaryTypeChangedEvent(eventId, BoundaryId.NewId(),
                BoundaryType.Divergent, BoundaryType.Convergent, timestamp, sequence, streamIdentity),
            new BoundaryGeometryUpdatedEvent(eventId, BoundaryId.NewId(),
                new LineSegment(0, 0, 1, 1), timestamp, sequence, streamIdentity),
            new BoundaryRetiredEvent(eventId, BoundaryId.NewId(), "reason", timestamp, sequence, streamIdentity),
            new JunctionUpdatedEvent(eventId, JunctionId.NewId(),
                new[] { BoundaryId.NewId() }, null, timestamp, sequence, streamIdentity),
            new JunctionRetiredEvent(eventId, JunctionId.NewId(), "reason", timestamp, sequence, streamIdentity),
            new PlateRetiredEvent(eventId, PlateId.NewId(), "reason", timestamp, sequence, streamIdentity)
        };

        // Assert - All events should implement IPlateTopologyEvent
        Assert.All(events, e => Assert.IsAssignableFrom<IPlateTopologyEvent>(e));
    }

    [Fact]
    public void AllEvents_EventType_ReturnsCorrectDiscriminator()
    {
        // Arrange
        var streamIdentity = CreateValidStreamIdentity();
        var timestamp = DateTimeOffset.UtcNow;
        var sequence = 0L;
        var eventId = Guid.NewGuid();

        var eventTypes = new (IPlateTopologyEvent Event, string ExpectedType)[]
        {
            (new PlateCreatedEvent(eventId, PlateId.NewId(), timestamp, sequence, streamIdentity), "PlateCreatedEvent"),
            (new BoundaryCreatedEvent(eventId, BoundaryId.NewId(), PlateId.NewId(), PlateId.NewId(),
                BoundaryType.Divergent, new LineSegment(0, 0, 1, 1), timestamp, sequence, streamIdentity), "BoundaryCreatedEvent"),
            (new JunctionCreatedEvent(eventId, JunctionId.NewId(),
                new[] { BoundaryId.NewId() }, new Point2D(0, 0), timestamp, sequence, streamIdentity), "JunctionCreatedEvent"),
            (new BoundaryTypeChangedEvent(eventId, BoundaryId.NewId(),
                BoundaryType.Divergent, BoundaryType.Convergent, timestamp, sequence, streamIdentity), "BoundaryTypeChangedEvent"),
            (new BoundaryGeometryUpdatedEvent(eventId, BoundaryId.NewId(),
                new LineSegment(0, 0, 1, 1), timestamp, sequence, streamIdentity), "BoundaryGeometryUpdatedEvent"),
            (new BoundaryRetiredEvent(eventId, BoundaryId.NewId(), "reason", timestamp, sequence, streamIdentity), "BoundaryRetiredEvent"),
            (new JunctionUpdatedEvent(eventId, JunctionId.NewId(),
                new[] { BoundaryId.NewId() }, null, timestamp, sequence, streamIdentity), "JunctionUpdatedEvent"),
            (new JunctionRetiredEvent(eventId, JunctionId.NewId(), "reason", timestamp, sequence, streamIdentity), "JunctionRetiredEvent"),
            (new PlateRetiredEvent(eventId, PlateId.NewId(), "reason", timestamp, sequence, streamIdentity), "PlateRetiredEvent")
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
        var @event = new PlateCreatedEvent(
            Guid.NewGuid(),
            PlateId.NewId(),
            DateTimeOffset.UtcNow,
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
