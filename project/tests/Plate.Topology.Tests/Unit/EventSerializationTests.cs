using System;
using System.Text;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using UnifyGeometry;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Serializers;
using Xunit;

namespace Plate.Topology.Tests.Unit;

/// <summary>
/// Unit tests for MessagePack event serialization per FR-012 (canonical encoding).
///
/// Tests cover:
/// - Roundtrip serialization for all 9 event types
/// - Determinism (same event produces identical bytes)
/// - No string keys in encoded payload
/// - Polymorphic deserialization via Deserialize(byte[])
/// </summary>
public class EventSerializationTests
{
    private static readonly TruthStreamIdentity TestStreamIdentity = new(
        "test",
        "main",
        2,
        Domain.Parse("geo.plates"),
        "0"
    );

    private static readonly CanonicalTick TestTick = new(0);

    private static readonly Guid TestEventId = Guid.Parse("01912345-6789-4321-9876-123456789012");

    private static PlateId CreateTestPlateId(Guid guid) => new PlateId(guid);
    private static BoundaryId CreateTestBoundaryId(Guid guid) => new BoundaryId(guid);
    private static JunctionId CreateTestJunctionId(Guid guid) => new JunctionId(guid);

    #region Roundtrip Tests

    [Fact]
    public void PlateCreatedEvent_Roundtrip_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var plateId = CreateTestPlateId(Guid.NewGuid());
        var originalEvent = TestEventFactory.PlateCreated(
            TestEventId,
            plateId,
            TestTick,
            1L,
            TestStreamIdentity
        );

        // Act
        var serialized = MessagePackEventSerializer.Serialize(originalEvent);
        var deserialized = MessagePackEventSerializer.Deserialize<PlateCreatedEvent>(serialized);

        // Assert
        Assert.Equal(originalEvent.EventId, deserialized.EventId);
        Assert.Equal(originalEvent.PlateId, deserialized.PlateId);
        Assert.Equal(originalEvent.Tick, deserialized.Tick);
        Assert.Equal(originalEvent.Sequence, deserialized.Sequence);
        Assert.Equal(originalEvent.StreamIdentity, deserialized.StreamIdentity);
    }

    [Fact]
    public void PlateRetiredEvent_Roundtrip_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var plateId = CreateTestPlateId(Guid.NewGuid());
        var reason = "Test retirement";
        var originalEvent = TestEventFactory.PlateRetired(
            TestEventId,
            plateId,
            reason,
            TestTick,
            9L,
            TestStreamIdentity
        );

        // Act
        var serialized = MessagePackEventSerializer.Serialize(originalEvent);
        var deserialized = MessagePackEventSerializer.Deserialize<PlateRetiredEvent>(serialized);

        // Assert
        Assert.Equal(originalEvent.EventId, deserialized.EventId);
        Assert.Equal(originalEvent.PlateId, deserialized.PlateId);
        Assert.Equal(originalEvent.Reason, deserialized.Reason);
        Assert.Equal(originalEvent.Tick, deserialized.Tick);
        Assert.Equal(originalEvent.Sequence, deserialized.Sequence);
        Assert.Equal(originalEvent.StreamIdentity, deserialized.StreamIdentity);
    }

    [Fact]
    public void BoundaryCreatedEvent_Roundtrip_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var boundaryId = CreateTestBoundaryId(Guid.NewGuid());
        var plateIdLeft = CreateTestPlateId(Guid.NewGuid());
        var plateIdRight = CreateTestPlateId(Guid.NewGuid());
        var geometry = new Segment2(0.0, 0.0, 10.0, 10.0);
        var originalEvent = TestEventFactory.BoundaryCreated(
            TestEventId,
            boundaryId,
            plateIdLeft,
            plateIdRight,
            BoundaryType.Divergent,
            geometry,
            TestTick,
            2L,
            TestStreamIdentity
        );

        // Act
        var serialized = MessagePackEventSerializer.Serialize(originalEvent);
        var deserialized = MessagePackEventSerializer.Deserialize<BoundaryCreatedEvent>(serialized);

        // Assert
        Assert.Equal(originalEvent.EventId, deserialized.EventId);
        Assert.Equal(originalEvent.BoundaryId, deserialized.BoundaryId);
        Assert.Equal(originalEvent.PlateIdLeft, deserialized.PlateIdLeft);
        Assert.Equal(originalEvent.PlateIdRight, deserialized.PlateIdRight);
        Assert.Equal(originalEvent.BoundaryType, deserialized.BoundaryType);
        Assert.Equal(originalEvent.Tick, deserialized.Tick);
        Assert.Equal(originalEvent.Sequence, deserialized.Sequence);
        Assert.Equal(originalEvent.StreamIdentity, deserialized.StreamIdentity);
        Assert.Equal(originalEvent.Geometry.GetType(), deserialized.Geometry.GetType());
    }

    [Fact]
    public void BoundaryTypeChangedEvent_Roundtrip_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var boundaryId = CreateTestBoundaryId(Guid.NewGuid());
        var oldType = BoundaryType.Divergent;
        var newType = BoundaryType.Convergent;
        var originalEvent = TestEventFactory.BoundaryTypeChanged(
            TestEventId,
            boundaryId,
            oldType,
            newType,
            TestTick,
            4L,
            TestStreamIdentity
        );

        // Act
        var serialized = MessagePackEventSerializer.Serialize(originalEvent);
        var deserialized = MessagePackEventSerializer.Deserialize<BoundaryTypeChangedEvent>(serialized);

        // Assert
        Assert.Equal(originalEvent.EventId, deserialized.EventId);
        Assert.Equal(originalEvent.BoundaryId, deserialized.BoundaryId);
        Assert.Equal(originalEvent.OldType, deserialized.OldType);
        Assert.Equal(originalEvent.NewType, deserialized.NewType);
        Assert.Equal(originalEvent.Tick, deserialized.Tick);
        Assert.Equal(originalEvent.Sequence, deserialized.Sequence);
        Assert.Equal(originalEvent.StreamIdentity, deserialized.StreamIdentity);
    }

    [Fact]
    public void BoundaryGeometryUpdatedEvent_Roundtrip_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var boundaryId = CreateTestBoundaryId(Guid.NewGuid());
        var points = new[]
        {
            new Point2(0.0, 0.0),
            new Point2(10.0, 10.0),
            new Point2(20.0, 20.0)
        };
        var newGeometry = new Polyline2(points);
        var originalEvent = TestEventFactory.BoundaryGeometryUpdated(
            TestEventId,
            boundaryId,
            newGeometry,
            TestTick,
            5L,
            TestStreamIdentity
        );

        // Act
        var serialized = MessagePackEventSerializer.Serialize(originalEvent);
        var deserialized = MessagePackEventSerializer.Deserialize<BoundaryGeometryUpdatedEvent>(serialized);

        // Assert
        Assert.Equal(originalEvent.EventId, deserialized.EventId);
        Assert.Equal(originalEvent.BoundaryId, deserialized.BoundaryId);
        Assert.Equal(originalEvent.NewGeometry.GetType(), deserialized.NewGeometry.GetType());
        Assert.Equal(originalEvent.Tick, deserialized.Tick);
        Assert.Equal(originalEvent.Sequence, deserialized.Sequence);
        Assert.Equal(originalEvent.StreamIdentity, deserialized.StreamIdentity);
    }

    [Fact]
    public void BoundaryRetiredEvent_Roundtrip_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var boundaryId = CreateTestBoundaryId(Guid.NewGuid());
        var reason = "Test retirement";
        var originalEvent = TestEventFactory.BoundaryRetired(
            TestEventId,
            boundaryId,
            reason,
            TestTick,
            6L,
            TestStreamIdentity
        );

        // Act
        var serialized = MessagePackEventSerializer.Serialize(originalEvent);
        var deserialized = MessagePackEventSerializer.Deserialize<BoundaryRetiredEvent>(serialized);

        // Assert
        Assert.Equal(originalEvent.EventId, deserialized.EventId);
        Assert.Equal(originalEvent.BoundaryId, deserialized.BoundaryId);
        Assert.Equal(originalEvent.Reason, deserialized.Reason);
        Assert.Equal(originalEvent.Tick, deserialized.Tick);
        Assert.Equal(originalEvent.Sequence, deserialized.Sequence);
        Assert.Equal(originalEvent.StreamIdentity, deserialized.StreamIdentity);
    }

    [Fact]
    public void JunctionCreatedEvent_Roundtrip_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var junctionId = CreateTestJunctionId(Guid.NewGuid());
        var boundaryIds = new[]
        {
            CreateTestBoundaryId(Guid.NewGuid()),
            CreateTestBoundaryId(Guid.NewGuid()),
            CreateTestBoundaryId(Guid.NewGuid())
        };
        var location = new Point2(5.0, 5.0);
        var originalEvent = TestEventFactory.JunctionCreated(
            TestEventId,
            junctionId,
            boundaryIds,
            location,
            TestTick,
            3L,
            TestStreamIdentity
        );

        // Act
        var serialized = MessagePackEventSerializer.Serialize(originalEvent);
        var deserialized = MessagePackEventSerializer.Deserialize<JunctionCreatedEvent>(serialized);

        // Assert
        Assert.Equal(originalEvent.EventId, deserialized.EventId);
        Assert.Equal(originalEvent.JunctionId, deserialized.JunctionId);
        Assert.Equal(originalEvent.BoundaryIds.Length, deserialized.BoundaryIds.Length);
        Assert.Equal(originalEvent.Location.X, deserialized.Location.X);
        Assert.Equal(originalEvent.Location.Y, deserialized.Location.Y);
        Assert.Equal(originalEvent.Tick, deserialized.Tick);
        Assert.Equal(originalEvent.Sequence, deserialized.Sequence);
        Assert.Equal(originalEvent.StreamIdentity, deserialized.StreamIdentity);
    }

    [Fact]
    public void JunctionUpdatedEvent_Roundtrip_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var junctionId = CreateTestJunctionId(Guid.NewGuid());
        var newBoundaryIds = new[]
        {
            CreateTestBoundaryId(Guid.NewGuid()),
            CreateTestBoundaryId(Guid.NewGuid())
        };
        var newLocation = new Point2(6.0, 6.0);
        var originalEvent = TestEventFactory.JunctionUpdated(
            TestEventId,
            junctionId,
            newBoundaryIds,
            newLocation,
            TestTick,
            7L,
            TestStreamIdentity
        );

        // Act
        var serialized = MessagePackEventSerializer.Serialize(originalEvent);
        var deserialized = MessagePackEventSerializer.Deserialize<JunctionUpdatedEvent>(serialized);

        // Assert
        Assert.Equal(originalEvent.EventId, deserialized.EventId);
        Assert.Equal(originalEvent.JunctionId, deserialized.JunctionId);
        Assert.Equal(originalEvent.NewBoundaryIds.Length, deserialized.NewBoundaryIds.Length);
        Assert.Equal(originalEvent.NewLocation.Value.X, deserialized.NewLocation.Value.X);
        Assert.Equal(originalEvent.NewLocation.Value.Y, deserialized.NewLocation.Value.Y);
        Assert.Equal(originalEvent.Tick, deserialized.Tick);
        Assert.Equal(originalEvent.Sequence, deserialized.Sequence);
        Assert.Equal(originalEvent.StreamIdentity, deserialized.StreamIdentity);
    }

    [Fact]
    public void JunctionRetiredEvent_Roundtrip_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var junctionId = CreateTestJunctionId(Guid.NewGuid());
        var reason = "Test retirement";
        var originalEvent = TestEventFactory.JunctionRetired(
            TestEventId,
            junctionId,
            reason,
            TestTick,
            8L,
            TestStreamIdentity
        );

        // Act
        var serialized = MessagePackEventSerializer.Serialize(originalEvent);
        var deserialized = MessagePackEventSerializer.Deserialize<JunctionRetiredEvent>(serialized);

        // Assert
        Assert.Equal(originalEvent.EventId, deserialized.EventId);
        Assert.Equal(originalEvent.JunctionId, deserialized.JunctionId);
        Assert.Equal(originalEvent.Reason, deserialized.Reason);
        Assert.Equal(originalEvent.Tick, deserialized.Tick);
        Assert.Equal(originalEvent.Sequence, deserialized.Sequence);
        Assert.Equal(originalEvent.StreamIdentity, deserialized.StreamIdentity);
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void Serialize_SameEventTwice_ProducesIdenticalBytes()
    {
        // Arrange
        var plateId = CreateTestPlateId(Guid.NewGuid());
        var @event = TestEventFactory.PlateCreated(
            TestEventId,
            plateId,
            TestTick,
            1L,
            TestStreamIdentity
        );

        // Act
        var bytes1 = MessagePackEventSerializer.Serialize(@event);
        var bytes2 = MessagePackEventSerializer.Serialize(@event);

        // Assert - determinism
        Assert.Equal(bytes1.Length, bytes2.Length);
        Assert.Equal(bytes1, bytes2);
    }

    [Fact]
    public void Serialize_WithPolyline_ProducesDeterministicBytes()
    {
        // Arrange
        var points = new[]
        {
            new Point2(0.0, 0.0),
            new Point2(10.0, 10.0),
            new Point2(20.0, 20.0)
        };
        var polyline = new Polyline2(points);
        var boundaryId = CreateTestBoundaryId(Guid.NewGuid());
        var @event = TestEventFactory.BoundaryGeometryUpdated(
            TestEventId,
            boundaryId,
            polyline,
            TestTick,
            5L,
            TestStreamIdentity
        );

        // Act
        var bytes1 = MessagePackEventSerializer.Serialize(@event);
        var bytes2 = MessagePackEventSerializer.Serialize(@event);

        // Assert - determinism
        Assert.Equal(bytes1.Length, bytes2.Length);
        Assert.Equal(bytes1, bytes2);
    }

    #endregion

    #region Polymorphic Deserialization Tests

    [Fact]
    public void Deserialize_Polymorphic_AllEventTypes_CorrectlyTypeDiscriminated()
    {
        // Test all event types polymorphically
        var plateCreated = TestEventFactory.PlateCreated(TestEventId, CreateTestPlateId(Guid.NewGuid()), TestTick, 1L, TestStreamIdentity);
        var plateRetired = TestEventFactory.PlateRetired(TestEventId, CreateTestPlateId(Guid.NewGuid()), "Test retirement", new CanonicalTick(1), 9L, TestStreamIdentity);
        var boundaryCreated = TestEventFactory.BoundaryCreated(TestEventId, CreateTestBoundaryId(Guid.NewGuid()), CreateTestPlateId(Guid.NewGuid()), CreateTestPlateId(Guid.NewGuid()), BoundaryType.Divergent, new Segment2(0.0, 0.0, 10.0, 10.0), new CanonicalTick(2), 2L, TestStreamIdentity);
        var boundaryTypeChanged = TestEventFactory.BoundaryTypeChanged(TestEventId, CreateTestBoundaryId(Guid.NewGuid()), BoundaryType.Divergent, BoundaryType.Convergent, new CanonicalTick(3), 4L, TestStreamIdentity);
        var boundaryGeometryUpdated = TestEventFactory.BoundaryGeometryUpdated(TestEventId, CreateTestBoundaryId(Guid.NewGuid()), new Segment2(0.0, 0.0, 20.0, 20.0), new CanonicalTick(4), 5L, TestStreamIdentity);
        var boundaryRetired = TestEventFactory.BoundaryRetired(TestEventId, CreateTestBoundaryId(Guid.NewGuid()), "Test retirement", new CanonicalTick(5), 6L, TestStreamIdentity);
        var junctionCreated = TestEventFactory.JunctionCreated(TestEventId, CreateTestJunctionId(Guid.NewGuid()), new[] { CreateTestBoundaryId(Guid.NewGuid()) }, new Point2(5.0, 5.0), new CanonicalTick(6), 3L, TestStreamIdentity);
        var junctionUpdated = TestEventFactory.JunctionUpdated(TestEventId, CreateTestJunctionId(Guid.NewGuid()), new[] { CreateTestBoundaryId(Guid.NewGuid()) }, new Point2(6.0, 6.0), new CanonicalTick(7), 7L, TestStreamIdentity);
        var junctionRetired = TestEventFactory.JunctionRetired(TestEventId, CreateTestJunctionId(Guid.NewGuid()), "Test retirement", new CanonicalTick(8), 8L, TestStreamIdentity);

        // Act - serialize and deserialize polymorphically
        // Use non-generic call to avoid ambiguity
        var plateCreatedBytes = MessagePackEventSerializer.Serialize(plateCreated);
        var plateRetiredBytes = MessagePackEventSerializer.Serialize(plateRetired);
        var boundaryCreatedBytes = MessagePackEventSerializer.Serialize(boundaryCreated);
        var boundaryTypeChangedBytes = MessagePackEventSerializer.Serialize(boundaryTypeChanged);
        var boundaryGeometryUpdatedBytes = MessagePackEventSerializer.Serialize(boundaryGeometryUpdated);
        var boundaryRetiredBytes = MessagePackEventSerializer.Serialize(boundaryRetired);
        var junctionCreatedBytes = MessagePackEventSerializer.Serialize(junctionCreated);
        var junctionUpdatedBytes = MessagePackEventSerializer.Serialize(junctionUpdated);
        var junctionRetiredBytes = MessagePackEventSerializer.Serialize(junctionRetired);

        var plateCreatedResult = MessagePackEventSerializer.Deserialize(plateCreatedBytes);
        var plateRetiredResult = MessagePackEventSerializer.Deserialize(plateRetiredBytes);
        var boundaryCreatedResult = MessagePackEventSerializer.Deserialize(boundaryCreatedBytes);
        var boundaryTypeChangedResult = MessagePackEventSerializer.Deserialize(boundaryTypeChangedBytes);
        var boundaryGeometryUpdatedResult = MessagePackEventSerializer.Deserialize(boundaryGeometryUpdatedBytes);
        var boundaryRetiredResult = MessagePackEventSerializer.Deserialize(boundaryRetiredBytes);
        var junctionCreatedResult = MessagePackEventSerializer.Deserialize(junctionCreatedBytes);
        var junctionUpdatedResult = MessagePackEventSerializer.Deserialize(junctionUpdatedBytes);
        var junctionRetiredResult = MessagePackEventSerializer.Deserialize(junctionRetiredBytes);

        // Assert - correct types
        Assert.IsType<PlateCreatedEvent>(plateCreatedResult);
        Assert.IsType<PlateRetiredEvent>(plateRetiredResult);
        Assert.IsType<BoundaryCreatedEvent>(boundaryCreatedResult);
        Assert.IsType<BoundaryTypeChangedEvent>(boundaryTypeChangedResult);
        Assert.IsType<BoundaryGeometryUpdatedEvent>(boundaryGeometryUpdatedResult);
        Assert.IsType<BoundaryRetiredEvent>(boundaryRetiredResult);
        Assert.IsType<JunctionCreatedEvent>(junctionCreatedResult);
        Assert.IsType<JunctionUpdatedEvent>(junctionUpdatedResult);
        Assert.IsType<JunctionRetiredEvent>(junctionRetiredResult);

        // Assert - values preserved (cast polymorphic result to specific type)
        Assert.IsType<PlateCreatedEvent>(plateCreatedResult);
        var casted = (PlateCreatedEvent)plateCreatedResult;
        Assert.Equal(plateCreated.EventId, casted.EventId);
        Assert.Equal(plateCreated.PlateId, casted.PlateId);
    }

    #endregion

    #region String Key Assertions

    [Fact]
    public void Encoding_ContainsNoStringKeys_Events()
    {
        // Arrange
        var @event = TestEventFactory.PlateCreated(
            TestEventId,
            CreateTestPlateId(Guid.NewGuid()),
            TestTick,
            1L,
            TestStreamIdentity
        );

        // Act
        var bytes = MessagePackEventSerializer.Serialize(@event);
        var bytesAsString = Encoding.UTF8.GetString(bytes);

        // Assert - no string keys (eventType string is expected, but not "EventType" map key)
        Assert.DoesNotContain("PlateId", bytesAsString);
        Assert.DoesNotContain("Tick", bytesAsString);
        Assert.DoesNotContain("Sequence", bytesAsString);
        Assert.DoesNotContain("VariantId", bytesAsString);
        Assert.DoesNotContain("BranchId", bytesAsString);
        Assert.DoesNotContain("LLevel", bytesAsString);
        Assert.DoesNotContain("Model", bytesAsString);
        // Note: We don't check for "EventType" string because it appears as the envelope string
    }

    [Fact]
    public void Encoding_ContainsNoStringKeys_Geometry()
    {
        // Arrange - test with event containing geometry
        var points = new[]
        {
            new Point2(0.0, 0.0),
            new Point2(10.0, 10.0),
            new Point2(20.0, 20.0)
        };
        var polyline = new Polyline2(points);
        var @event = TestEventFactory.BoundaryGeometryUpdated(
            TestEventId,
            CreateTestBoundaryId(Guid.NewGuid()),
            polyline,
            TestTick,
            1L,
            TestStreamIdentity
        );

        // Act
        var bytes = MessagePackEventSerializer.Serialize(@event);
        var bytesAsString = Encoding.UTF8.GetString(bytes);

        // Assert - no string keys for geometry (numeric discriminator expected)
        Assert.DoesNotContain("GeometryType", bytesAsString);
        Assert.DoesNotContain("Points", bytesAsString);
    }

    [Fact]
    public void Encoding_Domain_SerializedAsValue()
    {
        // Verify Domain is serialized as plain string (not as numeric)
        var @event = TestEventFactory.PlateCreated(
            TestEventId,
            CreateTestPlateId(Guid.NewGuid()),
            TestTick,
            1L,
            TestStreamIdentity
        );

        // Act
        var bytes = MessagePackEventSerializer.Serialize(@event);
        var bytesAsString = Encoding.UTF8.GetString(bytes);

        // Assert - Domain is plain string value
        Assert.Contains("geo.plates", bytesAsString);
        // Not as "Domain" key in a map
    }

    #endregion
}
