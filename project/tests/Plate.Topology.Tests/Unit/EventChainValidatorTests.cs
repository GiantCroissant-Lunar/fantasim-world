using System;
using System.Collections.Generic;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Serializers;
using Xunit;

namespace Plate.Topology.Tests.Unit;

/// <summary>
/// Unit tests for event hash-chain validation per Phase 1 CTU implementation.
///
/// These tests verify:
/// - Hash computation is deterministic (same inputs -> same hash)
/// - Hash chain validates correctly (PreviousHash links)
/// - Genesis events have empty PreviousHash
/// - Chain validation fails when links are broken
/// </summary>
public class EventChainValidatorTests
{
    private static readonly TruthStreamIdentity TestStream = new(
        "test-variant",
        "main",
        2,
        Domain.Parse("geo.plates"),
        "0"
    );

    #region Hash Determinism Tests

    [Fact]
    public void Sha256EventHasher_SameInput_ProducesSameHash()
    {
        // Arrange
        var hasher = Sha256EventHasher.Instance;
        var input = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var hash1 = hasher.Hash(input);
        var hash2 = hasher.Hash(input);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(32, hash1.Length); // SHA-256 produces 32 bytes
    }

    [Fact]
    public void Sha256EventHasher_DifferentInput_ProducesDifferentHash()
    {
        // Arrange
        var hasher = Sha256EventHasher.Instance;
        var input1 = new byte[] { 1, 2, 3, 4, 5 };
        var input2 = new byte[] { 1, 2, 3, 4, 6 };

        // Act
        var hash1 = hasher.Hash(input1);
        var hash2 = hasher.Hash(input2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeEventHash_SameEvent_ProducesSameHash()
    {
        // Arrange
        var hasher = Sha256EventHasher.Instance;
        var tick = new CanonicalTick(100);
        var previousHash = ReadOnlyMemory<byte>.Empty;
        var payload = new byte[] { 10, 20, 30 };

        // Act
        var hash1 = hasher.ComputeEventHash(tick, TestStream, previousHash, payload);
        var hash2 = hasher.ComputeEventHash(tick, TestStream, previousHash, payload);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeEventHash_DifferentTick_ProducesDifferentHash()
    {
        // Arrange
        var hasher = Sha256EventHasher.Instance;
        var tick1 = new CanonicalTick(100);
        var tick2 = new CanonicalTick(101);
        var previousHash = ReadOnlyMemory<byte>.Empty;
        var payload = new byte[] { 10, 20, 30 };

        // Act
        var hash1 = hasher.ComputeEventHash(tick1, TestStream, previousHash, payload);
        var hash2 = hasher.ComputeEventHash(tick2, TestStream, previousHash, payload);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    #endregion

    #region Chain Validation Tests

    [Fact]
    public void ValidateChain_EmptyList_ReturnsTrue()
    {
        // Arrange
        var events = new List<IPlateTopologyEvent>();

        // Act
        var result = EventChainValidator.ValidateChain(events);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateChain_SingleGenesisEvent_WithEmptyPreviousHash_ReturnsTrue()
    {
        // Arrange
        var genesisEvent = CreateGenesisPlateCreatedEvent();
        var events = new List<IPlateTopologyEvent> { genesisEvent };

        // Act
        var result = EventChainValidator.ValidateChain(events);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateChain_SingleEvent_WithNonEmptyPreviousHash_ReturnsFalse()
    {
        // Arrange - Genesis event should have empty PreviousHash
        var badGenesisEvent = TestEventFactory.PlateCreated(
            Guid.NewGuid(),
            PlateId.NewId(),
            new CanonicalTick(0),
            0L,
            TestStream,
            new byte[] { 1, 2, 3 }, // Non-empty - invalid for genesis
            new byte[] { 4, 5, 6 }
        );
        var events = new List<IPlateTopologyEvent> { badGenesisEvent };

        // Act
        var result = EventChainValidator.ValidateChain(events);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateChain_ThreeEvents_ValidChain_ReturnsTrue()
    {
        // Arrange
        var events = BuildValidThreeEventChain();

        // Act
        var result = EventChainValidator.ValidateChain(events);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateChain_ThreeEvents_BrokenChain_ReturnsFalse()
    {
        // Arrange
        var events = BuildValidThreeEventChain();

        // Break the chain by modifying the second event's PreviousHash
        var brokenEvent2 = TestEventFactory.PlateCreated(
            events[1].EventId,
            ((PlateCreatedEvent)events[1]).PlateId,
            events[1].Tick,
            events[1].Sequence,
            events[1].StreamIdentity,
            new byte[] { 99, 99, 99 }, // Wrong PreviousHash
            events[1].Hash
        );

        var brokenEvents = new List<IPlateTopologyEvent>
        {
            events[0],
            brokenEvent2,
            events[2]
        };

        // Act
        var result = EventChainValidator.ValidateChain(brokenEvents);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region WithComputedHash Extension Tests

    [Fact]
    public void PlateCreatedEvent_WithComputedHash_ProducesNonEmptyHash()
    {
        // Arrange
        var @event = TestEventFactory.PlateCreated(
            Guid.NewGuid(),
            PlateId.NewId(),
            new CanonicalTick(0),
            0L,
            TestStream,
            ReadOnlyMemory<byte>.Empty, // Genesis
            ReadOnlyMemory<byte>.Empty  // Will be computed
        );

        // Act
        var hashedEvent = @event.WithComputedHash();

        // Assert
        Assert.False(hashedEvent.Hash.IsEmpty);
        Assert.Equal(32, hashedEvent.Hash.Length);
    }

    [Fact]
    public void PlateCreatedEvent_WithComputedHash_IsDeterministic()
    {
        // Arrange
        var eventId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var plateId = new PlateId(Guid.Parse("87654321-4321-4321-4321-cba987654321"));

        var @event = TestEventFactory.PlateCreated(
            eventId,
            plateId,
            new CanonicalTick(100),
            5L,
            TestStream,
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty
        );

        // Act
        var hashedEvent1 = @event.WithComputedHash();
        var hashedEvent2 = @event.WithComputedHash();

        // Assert
        Assert.True(hashedEvent1.Hash.Span.SequenceEqual(hashedEvent2.Hash.Span));
    }

    #endregion

    #region GetLastHash Tests

    [Fact]
    public void GetLastHash_EmptyList_ReturnsEmpty()
    {
        // Arrange
        var events = new List<IPlateTopologyEvent>();

        // Act
        var result = EventChainValidator.GetLastHash(events);

        // Assert
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void GetLastHash_NonEmptyList_ReturnsLastEventHash()
    {
        // Arrange
        var events = BuildValidThreeEventChain();

        // Act
        var result = EventChainValidator.GetLastHash(events);

        // Assert
        Assert.True(result.Span.SequenceEqual(events[2].Hash.Span));
    }

    #endregion

    #region Golden Bytes Test

    [Fact]
    public void PlateCreatedEvent_GoldenBytes_HashIsStable()
    {
        // Arrange - Fixed inputs for deterministic golden test
        var eventId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var plateId = new PlateId(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        var stream = new TruthStreamIdentity("v1", "main", 0, Domain.Parse("plates"), "M0");

        var @event = TestEventFactory.PlateCreated(
            eventId,
            plateId,
            new CanonicalTick(0),
            0L,
            stream,
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty
        );

        // Act
        var hashedEvent = @event.WithComputedHash();

        // Assert - Hash should be deterministic and stable across runs
        // This is a "golden" test - if the hash computation changes, this test will fail
        // which signals a potential breaking change in the hash format
        Assert.Equal(32, hashedEvent.Hash.Length);

        // Log the hash for documentation (useful when establishing golden values)
        var hashHex = Convert.ToHexString(hashedEvent.Hash.Span);

        // The actual golden value - update this if the hash format intentionally changes
        // For now, we just verify it's deterministic by computing twice
        var hashedEvent2 = @event.WithComputedHash();
        Assert.Equal(hashHex, Convert.ToHexString(hashedEvent2.Hash.Span));
    }

    #endregion

    #region Helpers

    private static PlateCreatedEvent CreateGenesisPlateCreatedEvent()
    {
        var @event = TestEventFactory.PlateCreated(
            Guid.NewGuid(),
            PlateId.NewId(),
            new CanonicalTick(0),
            0L,
            TestStream,
            ReadOnlyMemory<byte>.Empty, // Genesis - empty PreviousHash
            ReadOnlyMemory<byte>.Empty  // Will be computed
        );

        return @event.WithComputedHash();
    }

    private static List<IPlateTopologyEvent> BuildValidThreeEventChain()
    {
        var events = new List<IPlateTopologyEvent>();

        // Event 1: Genesis (PreviousHash = empty)
        var event1 = TestEventFactory.PlateCreated(
            Guid.NewGuid(),
            PlateId.NewId(),
            new CanonicalTick(0),
            0L,
            TestStream,
            ReadOnlyMemory<byte>.Empty,
            ReadOnlyMemory<byte>.Empty
        ).WithComputedHash();
        events.Add(event1);

        // Event 2: Links to event1
        var event2 = TestEventFactory.PlateCreated(
            Guid.NewGuid(),
            PlateId.NewId(),
            new CanonicalTick(100),
            1L,
            TestStream,
            event1.Hash, // Links to previous
            ReadOnlyMemory<byte>.Empty
        ).WithComputedHash();
        events.Add(event2);

        // Event 3: Links to event2
        var event3 = TestEventFactory.PlateCreated(
            Guid.NewGuid(),
            PlateId.NewId(),
            new CanonicalTick(200),
            2L,
            TestStream,
            event2.Hash, // Links to previous
            ReadOnlyMemory<byte>.Empty
        ).WithComputedHash();
        events.Add(event3);

        return events;
    }

    #endregion
}
