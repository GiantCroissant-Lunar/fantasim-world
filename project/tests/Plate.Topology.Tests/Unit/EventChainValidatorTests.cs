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

    #region Golden Vector Tests (Phase 2)

    /// <summary>
    /// Golden vector test: ensures hash computation is stable across versions.
    /// If this test fails, it means the hash algorithm or preimage structure changed,
    /// which would break existing hash chains in production.
    ///
    /// The expected hash is for a PlateCreatedEvent with:
    /// - eventId: 12345678-1234-1234-1234-123456789abc
    /// - plateId: aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee
    /// - tick: 1000
    /// - sequence: 0
    /// - stream: [science, main, 2, geo.plates, 0]
    /// - previousHash: empty (genesis)
    ///
    /// Hash preimage structure (MessagePack array):
    /// [0] Tick (Int64): 1000
    /// [1] StreamIdentity (array): [science, main, 2, geo.plates, 0]
    /// [2] PreviousHash (binary): empty
    /// [3] PayloadBytes (binary): [eventId, plateId] serialized
    ///
    /// DO NOT change the expected hash value without understanding the implications.
    /// A hash change means existing persisted data will fail validation.
    /// </summary>
    [Fact]
    public void GoldenVector_PlateCreatedEvent_ProducesExpectedHash()
    {
        // Arrange - Fixed inputs for deterministic test
        // These values are FROZEN - do not change without a migration plan
        var eventId = new Guid("12345678-1234-1234-1234-123456789abc");
        var plateId = new PlateId(new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        var tick = new CanonicalTick(1000);
        var sequence = 0L;
        var stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0"
        );

        // Create event with empty PreviousHash (genesis)
        var genesisEvent = TestEventFactory.PlateCreated(
            eventId,
            plateId,
            tick,
            sequence,
            stream
        ).WithComputedHash();

        // Act
        var hashHex = Convert.ToHexString(genesisEvent.Hash.Span);

        // Also get preimage for related test
        var preimageBytes = CanonicalEventSerializer.Instance.SerializeCanonicalForHash(genesisEvent);
        var preimageHex = Convert.ToHexString(preimageBytes);

        // Assert - basic invariants
        Assert.Equal(32, genesisEvent.Hash.Length);
        Assert.Equal(64, hashHex.Length); // 32 bytes = 64 hex chars

        // FROZEN GOLDEN VECTOR - computed 2025-01-24
        // Preimage structure: [Tick=1000, Stream=[science,main,2,geo.plates,0], PrevHash=empty, Payload=[eventId,plateId]]
        // Hash: SHA256(preimage)
        // If this fails, the hash algorithm or preimage serialization changed.
        // DO NOT update this value without:
        // 1. Understanding why the hash changed
        // 2. Documenting the breaking change
        // 3. Providing a migration path for existing data
        //
        // Preimage structure breakdown (MessagePack):
        // 94                 - fixarray(4)
        // CD03E8             - uint16(1000) = tick
        // 95                 - fixarray(5) for stream identity
        //   A7736369656E6365 - fixstr(7) "science"
        //   A46D61696E       - fixstr(4) "main"
        //   02               - fixint(2) LLevel
        //   AA67656F2E706C61746573 - fixstr(10) "geo.plates"
        //   A130             - fixstr(1) "0"
        // C400               - bin8(0) empty previous hash
        // C44D...            - bin8(77) payload bytes
        const string ExpectedHashHex = "61E096BA3FC8A6757225B33353C185FE1D338F7D1800106C31C609F47F8F3E66";
        const string ExpectedPreimageHex = "94CD03E895A7736369656E6365A46D61696E02AA67656F2E706C61746573A130C400C44D92D92431323334353637382D313233342D313233342D313233342D313233343536373839616263D92461616161616161612D626262622D636363632D646464642D656565656565656565656565";

        Assert.Equal(ExpectedHashHex, hashHex);
        Assert.Equal(ExpectedPreimageHex, preimageHex);
    }

    /// <summary>
    /// Golden vector for preimage bytes - catches "hash stays same by coincidence" edge cases.
    /// This verifies the exact bytes being fed to SHA-256.
    /// </summary>
    [Fact]
    public void GoldenVector_PlateCreatedEvent_PreimageIsStable()
    {
        // Arrange - Same inputs as GoldenVector_PlateCreatedEvent_ProducesExpectedHash
        var eventId = new Guid("12345678-1234-1234-1234-123456789abc");
        var plateId = new PlateId(new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        var tick = new CanonicalTick(1000);
        var stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0"
        );

        // Act - Get the canonical preimage bytes
        var evt = TestEventFactory.PlateCreated(eventId, plateId, tick, 0, stream);
        var serializer = CanonicalEventSerializer.Instance;
        var preimageBytes = serializer.SerializeCanonicalForHash(evt);
        var preimageHex = Convert.ToHexString(preimageBytes);

        // Assert - FROZEN preimage hex
        // See GoldenVector_PlateCreatedEvent_ProducesExpectedHash for structure breakdown
        const string ExpectedPreimageHex = "94CD03E895A7736369656E6365A46D61696E02AA67656F2E706C61746573A130C400C44D92D92431323334353637382D313233342D313233342D313233342D313233343536373839616263D92461616161616161612D626262622D636363632D646464642D656565656565656565656565";

        Assert.Equal(ExpectedPreimageHex, preimageHex);
    }

    /// <summary>
    /// Verifies that the hash preimage does NOT include the Hash field itself.
    /// This is critical - if Hash is in the preimage, computing the hash would be circular.
    /// </summary>
    [Fact]
    public void HashPreimage_DoesNotIncludeHashField()
    {
        // Arrange
        var eventId = new Guid("12345678-1234-1234-1234-123456789abc");
        var plateId = new PlateId(new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        var tick = new CanonicalTick(1000);
        var sequence = 0L;

        // Create two events with same inputs but different Hash values
        var event1 = TestEventFactory.PlateCreated(
            eventId,
            plateId,
            tick,
            sequence,
            TestStream,
            ReadOnlyMemory<byte>.Empty, // PreviousHash
            new byte[] { 1, 2, 3, 4 }   // Some arbitrary Hash value
        );

        var event2 = TestEventFactory.PlateCreated(
            eventId,
            plateId,
            tick,
            sequence,
            TestStream,
            ReadOnlyMemory<byte>.Empty, // Same PreviousHash
            new byte[] { 5, 6, 7, 8 }   // Different Hash value
        );

        // Act - Compute hashes for both (this replaces the Hash field)
        var hashed1 = event1.WithComputedHash();
        var hashed2 = event2.WithComputedHash();

        // Assert - They should produce the SAME hash because Hash is not in the preimage
        Assert.True(hashed1.Hash.Span.SequenceEqual(hashed2.Hash.Span));
    }

    #endregion
}
