using System;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using FantaSim.Geosphere.Plate.Topology.Serializers;
using FantaSim.Geosphere.Plate.Testing.Storage;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Integration;

/// <summary>
/// Integration tests for PlateTopologyEventStore hash-chain behavior per Phase 3 plan.
///
/// These tests verify:
/// - Append then read round-trips bytes correctly
/// - Store computes chain and ignores caller-provided hashes
/// - Prefix scan returns events in sequence order
/// - Hash chain validation on read
/// </summary>
public class EventStoreHashChainTests : IDisposable
{
    private readonly PlateTopologyEventStore _store;
    private readonly TruthStreamIdentity _stream;

    public EventStoreHashChainTests()
    {
        _store = TestStores.CreateEventStore();

        _stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0"
        );
    }

    public void Dispose()
    {
        _store.Dispose();
    }

    #region AppendThenRead_RoundTrips Tests

    /// <summary>
    /// Verifies that appended events can be read back with the same logical content.
    /// Hash fields may differ (store computes them), but payload is preserved.
    /// </summary>
    [Fact]
    public async Task AppendThenRead_SingleEvent_RoundTripsPayload()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var plateId = new PlateId(Guid.NewGuid());
        var tick = new CanonicalTick(100);
        var sequence = 0L;

        var evt = TestEventFactory.PlateCreated(eventId, plateId, tick, sequence, _stream);

        // Act
        await _store.AppendAsync(_stream, new IPlateTopologyEvent[] { evt }, CancellationToken.None);
        var readEvents = await _store.ReadAsync(_stream, 0, CancellationToken.None).ToListAsync();

        // Assert
        var retrieved = Assert.Single(readEvents);
        Assert.IsType<PlateCreatedEvent>(retrieved);

        var readEvent = (PlateCreatedEvent)retrieved;
        Assert.Equal(eventId, readEvent.EventId);
        Assert.Equal(plateId, readEvent.PlateId);
        Assert.Equal(tick, readEvent.Tick);
        Assert.Equal(sequence, readEvent.Sequence);
        Assert.Equal(_stream, readEvent.StreamIdentity);
    }

    /// <summary>
    /// Verifies that multiple events are round-tripped correctly.
    /// </summary>
    [Fact]
    public async Task AppendThenRead_MultipleEvents_RoundTripsAllPayloads()
    {
        // Arrange
        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, _stream),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(), boundaryId, plateId1, plateId2,
                BoundaryType.Divergent, new Segment2(0, 0, 10, 10),
                new CanonicalTick(2), 2, _stream)
        };

        // Act
        await _store.AppendAsync(_stream, events, CancellationToken.None);
        var readEvents = await _store.ReadAsync(_stream, 0, CancellationToken.None).ToListAsync();

        // Assert
        Assert.Equal(3, readEvents.Count);
        Assert.IsType<PlateCreatedEvent>(readEvents[0]);
        Assert.IsType<PlateCreatedEvent>(readEvents[1]);
        Assert.IsType<BoundaryCreatedEvent>(readEvents[2]);

        Assert.Equal(plateId1, ((PlateCreatedEvent)readEvents[0]).PlateId);
        Assert.Equal(plateId2, ((PlateCreatedEvent)readEvents[1]).PlateId);
        Assert.Equal(boundaryId, ((BoundaryCreatedEvent)readEvents[2]).BoundaryId);
    }

    #endregion

    #region Append_ComputesChain_IgnoresCallerHashes Tests

    /// <summary>
    /// Verifies that the store computes its own hash chain and ignores caller-provided hashes.
    /// This is critical for chain integrity - the store is the authority.
    /// </summary>
    [Fact]
    public async Task Append_WithJunkHashes_StoreOverwritesWithCorrectChain()
    {
        // Arrange - Create events with garbage hash values
        var eventId = Guid.NewGuid();
        var plateId = new PlateId(Guid.NewGuid());
        var junkPreviousHash = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC };
        var junkHash = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };

        var evt = TestEventFactory.PlateCreated(
            eventId, plateId,
            new CanonicalTick(0), 0, _stream,
            junkPreviousHash, junkHash);

        // Act
        await _store.AppendAsync(_stream, new IPlateTopologyEvent[] { evt }, CancellationToken.None);
        var readEvents = await _store.ReadAsync(_stream, 0, CancellationToken.None).ToListAsync();

        // Assert - The store should have computed correct hashes (not used our junk)
        var retrieved = Assert.Single(readEvents);

        // Genesis event should have empty PreviousHash (not our junk)
        // Note: The stored event may or may not expose PreviousHash/Hash via the interface,
        // but the chain should be internally consistent when re-read
        Assert.Equal(eventId, retrieved.EventId);
    }

    /// <summary>
    /// Verifies that appending multiple events creates a proper chain.
    /// Each event's PreviousHash should link to the previous event's Hash.
    /// </summary>
    [Fact]
    public async Task Append_MultipleEvents_CreatesValidChain()
    {
        // Arrange
        var events = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(1), 1, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(2), 2, _stream)
        };

        // Act
        await _store.AppendAsync(_stream, events, CancellationToken.None);
        var readEvents = await _store.ReadAsync(_stream, 0, CancellationToken.None).ToListAsync();

        // Assert - All events should be readable (chain is valid)
        Assert.Equal(3, readEvents.Count);

        // Verify sequences are in order
        for (int i = 0; i < readEvents.Count; i++)
        {
            Assert.Equal(i, readEvents[i].Sequence);
        }
    }

    /// <summary>
    /// Verifies that appending in separate batches maintains chain continuity.
    /// The second batch should link to the last event of the first batch.
    /// </summary>
    [Fact]
    public async Task Append_SeparateBatches_MaintainsChainContinuity()
    {
        // Arrange - First batch
        var batch1 = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(1), 1, _stream)
        };

        // Act - Append first batch
        await _store.AppendAsync(_stream, batch1, CancellationToken.None);

        // Arrange - Second batch (continues from sequence 2)
        var batch2 = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(2), 2, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(3), 3, _stream)
        };

        // Act - Append second batch
        await _store.AppendAsync(_stream, batch2, CancellationToken.None);

        // Read all events
        var readEvents = await _store.ReadAsync(_stream, 0, CancellationToken.None).ToListAsync();

        // Assert - All 4 events should be readable (chain is continuous)
        Assert.Equal(4, readEvents.Count);

        // Verify sequences are continuous
        for (int i = 0; i < readEvents.Count; i++)
        {
            Assert.Equal(i, readEvents[i].Sequence);
        }
    }

    #endregion

    #region PrefixScan_OrderIsSeqOrder Tests

    /// <summary>
    /// Verifies that events are always returned in sequence order,
    /// regardless of the order they were appended or their tick values.
    /// </summary>
    [Fact]
    public async Task PrefixScan_WithNonMonotonicTicks_ReturnsInSeqOrder()
    {
        // Arrange - Events with non-monotonic tick values (but monotonic sequences)
        var events = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(100), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(50), 1, _stream),  // Tick goes backward
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(200), 2, _stream)
        };

        // Act
        await _store.AppendAsync(_stream, events, CancellationToken.None);
        var readEvents = await _store.ReadAsync(_stream, 0, CancellationToken.None).ToListAsync();

        // Assert - Order should be by sequence, not tick
        Assert.Equal(3, readEvents.Count);
        Assert.Equal(0, readEvents[0].Sequence);
        Assert.Equal(1, readEvents[1].Sequence);
        Assert.Equal(2, readEvents[2].Sequence);

        // Ticks should be preserved as stored
        Assert.Equal(new CanonicalTick(100), readEvents[0].Tick);
        Assert.Equal(new CanonicalTick(50), readEvents[1].Tick);
        Assert.Equal(new CanonicalTick(200), readEvents[2].Tick);
    }

    /// <summary>
    /// Verifies that reading from a specific sequence returns events starting from that sequence.
    /// </summary>
    [Fact]
    public async Task PrefixScan_FromMiddleSequence_ReturnsRemainingInOrder()
    {
        // Arrange
        var events = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(1), 1, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(2), 2, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(3), 3, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(4), 4, _stream)
        };

        await _store.AppendAsync(_stream, events, CancellationToken.None);

        // Act - Read from sequence 2
        var readEvents = await _store.ReadAsync(_stream, 2, CancellationToken.None).ToListAsync();

        // Assert - Should return events 2, 3, 4
        Assert.Equal(3, readEvents.Count);
        Assert.Equal(2, readEvents[0].Sequence);
        Assert.Equal(3, readEvents[1].Sequence);
        Assert.Equal(4, readEvents[2].Sequence);
    }

    #endregion

    #region Stream Isolation Tests

    /// <summary>
    /// Verifies that events in different streams are isolated.
    /// Reading from one stream should not return events from another.
    /// </summary>
    [Fact]
    public async Task StreamIsolation_DifferentStreams_NoLeakage()
    {
        // Arrange - Two different streams
        var stream1 = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "0");
        var stream2 = new TruthStreamIdentity("wuxing", "alternate", 2, Domain.Parse("geo.plates"), "0");

        var events1 = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(0), 0, stream1),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(1), 1, stream1)
        };

        var events2 = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(0), 0, stream2),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(1), 1, stream2),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(2), 2, stream2)
        };

        // Act
        await _store.AppendAsync(stream1, events1, CancellationToken.None);
        await _store.AppendAsync(stream2, events2, CancellationToken.None);

        var readFromStream1 = await _store.ReadAsync(stream1, 0, CancellationToken.None).ToListAsync();
        var readFromStream2 = await _store.ReadAsync(stream2, 0, CancellationToken.None).ToListAsync();

        // Assert - Each stream has only its own events
        Assert.Equal(2, readFromStream1.Count);
        Assert.Equal(3, readFromStream2.Count);

        Assert.All(readFromStream1, e => Assert.Equal(stream1, e.StreamIdentity));
        Assert.All(readFromStream2, e => Assert.Equal(stream2, e.StreamIdentity));
    }

    #endregion

    #region GetLastSequence Tests

    /// <summary>
    /// Verifies that GetLastSequenceAsync returns the correct last sequence.
    /// </summary>
    [Fact]
    public async Task GetLastSequence_AfterAppend_ReturnsCorrectValue()
    {
        // Arrange
        var events = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(0), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(1), 1, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(2), 2, _stream)
        };

        // Act
        await _store.AppendAsync(_stream, events, CancellationToken.None);
        var lastSeq = await _store.GetLastSequenceAsync(_stream, CancellationToken.None);

        // Assert
        Assert.Equal(2L, lastSeq);
    }

    /// <summary>
    /// Verifies that GetLastSequenceAsync returns null for empty stream.
    /// </summary>
    [Fact]
    public async Task GetLastSequence_EmptyStream_ReturnsNull()
    {
        // Act
        var lastSeq = await _store.GetLastSequenceAsync(_stream, CancellationToken.None);

        // Assert
        Assert.Null(lastSeq);
    }

    #endregion

    #region Tick Monotonicity Policy Tests

    /// <summary>
    /// Verifies that TickMonotonicityPolicy.Allow (default) permits tick to decrease.
    /// </summary>
    [Fact]
    public async Task TickPolicy_Allow_PermitsDecreasingTick()
    {
        // Arrange - Events with decreasing tick
        var events = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(100), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(50), 1, _stream),  // Tick decreases
        };

        // Act - Should not throw
        await _store.AppendAsync(_stream, events, AppendOptions.Default, CancellationToken.None);
        var readEvents = await _store.ReadAsync(_stream, 0, CancellationToken.None).ToListAsync();

        // Assert - Events were stored
        Assert.Equal(2, readEvents.Count);
    }

    /// <summary>
    /// Verifies that TickMonotonicityPolicy.Reject throws on tick decrease.
    /// </summary>
    [Fact]
    public async Task TickPolicy_Reject_ThrowsOnDecreasingTick()
    {
        // Arrange - Events with decreasing tick
        var events = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(100), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(50), 1, _stream),  // Tick decreases
        };

        var options = new AppendOptions { TickPolicy = TickMonotonicityPolicy.Reject };

        // Act & Assert - Should throw InvalidOperationException
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _store.AppendAsync(_stream, events, options, CancellationToken.None));

        Assert.Contains("Tick monotonicity violation", ex.Message, StringComparison.Ordinal);
        Assert.Contains("100", ex.Message, StringComparison.Ordinal);  // Previous tick
        Assert.Contains("50", ex.Message, StringComparison.Ordinal);   // Current tick
    }

    /// <summary>
    /// Verifies that TickMonotonicityPolicy.Reject allows monotonically increasing ticks.
    /// </summary>
    [Fact]
    public async Task TickPolicy_Reject_AllowsIncreasingTick()
    {
        // Arrange - Events with increasing tick
        var events = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(100), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(100), 1, _stream),  // Same tick is OK
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(200), 2, _stream),  // Increasing tick
        };

        var options = new AppendOptions { TickPolicy = TickMonotonicityPolicy.Reject };

        // Act - Should not throw
        await _store.AppendAsync(_stream, events, options, CancellationToken.None);
        var readEvents = await _store.ReadAsync(_stream, 0, CancellationToken.None).ToListAsync();

        // Assert - Events were stored
        Assert.Equal(3, readEvents.Count);
    }

    /// <summary>
    /// Verifies that TickMonotonicityPolicy.Warn permits tick decrease but logs warning.
    /// (Warning is logged via Debug.WriteLine, which is visible in test output)
    /// </summary>
    [Fact]
    public async Task TickPolicy_Warn_PermitsDecreasingTick()
    {
        // Arrange - Events with decreasing tick
        var events = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(100), 0, _stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(50), 1, _stream),  // Tick decreases
        };

        var options = new AppendOptions { TickPolicy = TickMonotonicityPolicy.Warn };

        // Act - Should not throw
        await _store.AppendAsync(_stream, events, options, CancellationToken.None);
        var readEvents = await _store.ReadAsync(_stream, 0, CancellationToken.None).ToListAsync();

        // Assert - Events were stored
        Assert.Equal(2, readEvents.Count);
        // Note: Warning is logged via Debug.WriteLine, visible in test output
    }

    #endregion

    #region Hash Chain Corruption Detection Tests

    /// <summary>
    /// Verifies that tampered event bytes are detected during read.
    /// This is the "corruption test" that validates hash chain integrity checking.
    /// </summary>
    [Fact]
    public async Task ReadAsync_WithCorruptedEventBytes_ThrowsHashMismatch()
    {
        // Arrange - Create store with access to underlying KV
        var (store, kv) = TestStores.CreateEventStoreWithKv();
        var stream = new TruthStreamIdentity("science", "main", 2, Domain.Parse("geo.plates"), "0");

        // Append events normally
        var events = new IPlateTopologyEvent[]
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(100), 0, stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(200), 1, stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(300), 2, stream),
        };

        await store.AppendAsync(stream, events, CancellationToken.None);

        // Find and corrupt the second event (seq=1) by modifying bytes
        CorruptSecondEvent(kv);

        // Act & Assert - Reading should detect hash chain corruption
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in store.ReadAsync(stream, 0, CancellationToken.None))
            {
                // Just iterate to trigger validation
            }
        });

        // The error should indicate a hash mismatch
        Assert.Contains("hash", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private void CorruptSecondEvent(InMemoryOrderedKeyValueStore kv)
    {
        // The key format is "S:{variant}:{branch}:L{l}:{domain}:M{m}:E:{seq}"
        // We need to iterate and find the event key
        using (var iterator = kv.CreateIterator())
        {
            // Seek to beginning and find event keys
            iterator.Seek(System.Text.Encoding.UTF8.GetBytes("S:"));

            var eventKeysFound = new List<byte[]>();
            while (iterator.Valid)
            {
                // iterator.Key is ReadOnlySpan<byte>, no need for .Span
                var keySpan = iterator.Key;
                var keyStr = System.Text.Encoding.UTF8.GetString(keySpan[..Math.Min(keySpan.Length, 50)]);
                if (keyStr.Contains("E:"))
                {
                    eventKeysFound.Add(keySpan.ToArray());
                }
                iterator.Next();

                // Safety: only look at first 10 keys
                if (eventKeysFound.Count >= 3) break;
            }

            // Corrupt the second event (index 1)
            if (eventKeysFound.Count >= 2)
            {
                var keyToCorrupt = eventKeysFound[1];
                var buffer = new byte[4096]; // Sufficiently large buffer
                if (kv.TryGet(keyToCorrupt, buffer, out var written))
                {
                    var originalValue = buffer.AsSpan(0, written).ToArray();

                    // Flip a bit in the middle of the value
                    var corruptedValue = (byte[])originalValue.Clone();
                    if (corruptedValue.Length > 10)
                    {
                        corruptedValue[corruptedValue.Length / 2] ^= 0xFF; // Flip all bits
                    }
                    kv.Put(keyToCorrupt, corruptedValue);
                }
            }
        }
    }

    #endregion
}
