using System;
using MessagePack;
using UnifyGeometry;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Serializers;

namespace FantaSim.Geosphere.Plate.Topology.Tests;

/// <summary>
/// Golden tests for MessagePackEventSerializer.
///
/// These tests verify that serializer produces deterministic output for event serialization.
/// For now, we do basic round-trip tests without checking golden hashes,
/// since golden fixture infrastructure is not yet in place.
/// </summary>
public class SerializerGoldenTests
{
    [Fact]
    public void Serialize_PlateCreatedEvent_RoundTrip_Succeeds()
    {
        // Arrange
        var plateId = PlateId.NewId();
        var eventId = Guid.NewGuid();
        var tick = new CanonicalTick(100L);

        var @event = new PlateCreatedEvent(
            eventId,
            plateId,
            tick,
            0L,
            new TruthStreamIdentity("test", "main", 2, Domain.Parse("geo.plates"), "0"),
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty
        );

        // Act
        var serialized = MessagePackEventSerializer.Serialize(@event);
        var deserialized = MessagePackEventSerializer.Deserialize<PlateCreatedEvent>(serialized);

        // Assert
        Assert.Equal(eventId, deserialized.EventId);
        Assert.Equal(plateId, deserialized.PlateId);
        Assert.Equal(tick.Value, deserialized.Tick.Value);
        Assert.Equal(0L, deserialized.Sequence);
    }

    [Fact]
    public void Serialize_PlateRetiredEvent_RoundTrip_Succeeds()
    {
        // Arrange
        var plateId = PlateId.NewId();
        var eventId = Guid.NewGuid();
        var tick = new CanonicalTick(200L);

        var @event = new PlateRetiredEvent(
            eventId,
            plateId,
            null,
            tick,
            1L,
            new TruthStreamIdentity("test", "main", 2, Domain.Parse("geo.plates"), "0"),
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty
        );

        // Act
        var serialized = MessagePackEventSerializer.Serialize(@event);
        var deserialized = MessagePackEventSerializer.Deserialize<PlateRetiredEvent>(serialized);

        // Assert
        Assert.Equal(eventId, deserialized.EventId);
        Assert.Equal(plateId, deserialized.PlateId);
        Assert.Equal(tick.Value, deserialized.Tick.Value);
        Assert.Equal(1L, deserialized.Sequence);
    }

    [Fact]
    public void Serialize_BoundaryCreatedEvent_RoundTrip_Succeeds()
    {
        // Arrange
        var leftPlateId = PlateId.NewId();
        var rightPlateId = PlateId.NewId();
        var boundaryId = BoundaryId.NewId();
        var eventId = Guid.NewGuid();
        var tick = new CanonicalTick(300L);

        var @event = new BoundaryCreatedEvent(
            eventId,
            boundaryId,
            leftPlateId,
            rightPlateId,
            BoundaryType.Divergent,
            null!,
            tick,
            2L,
            new TruthStreamIdentity("test", "main", 2, Domain.Parse("geo.plates"), "0"),
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty
        );

        // Act
        var serialized = MessagePackEventSerializer.Serialize(@event);
        var deserialized = MessagePackEventSerializer.Deserialize<BoundaryCreatedEvent>(serialized);

        // Assert
        Assert.Equal(eventId, deserialized.EventId);
        Assert.Equal(boundaryId, deserialized.BoundaryId);
        Assert.Equal(tick.Value, deserialized.Tick.Value);
        Assert.Equal(2L, deserialized.Sequence);
    }

    [Fact]
    public void Deserialize_Polymorphic_DispatchesCorrectly()
    {
        // Arrange
        var plateId = PlateId.NewId();
        var eventId = Guid.NewGuid();
        var tick = new CanonicalTick(400L);

        var @event = new PlateCreatedEvent(
            eventId,
            plateId,
            tick,
            0L,
            new TruthStreamIdentity("test", "main", 2, Domain.Parse("geo.plates"), "0"),
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty
        );

        // Act - serialize as IPlateTopologyEvent, deserialize as IPlateTopologyEvent
        var serialized = MessagePackEventSerializer.Serialize(@event);
        var deserialized = MessagePackEventSerializer.Deserialize(serialized);

        // Assert
        Assert.Equal(nameof(PlateCreatedEvent), deserialized.EventType);
        var createdEvent = Assert.IsType<PlateCreatedEvent>(deserialized);
        Assert.Equal(eventId, createdEvent.EventId);
        Assert.Equal(plateId, createdEvent.PlateId);
    }
}
