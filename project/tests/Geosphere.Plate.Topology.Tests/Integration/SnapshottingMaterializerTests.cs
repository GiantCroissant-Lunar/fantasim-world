using System.Buffers.Binary;
using System.Linq;
using System.Text;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Materializer;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Integration;

public sealed class SnapshottingMaterializerTests
{
    /// <summary>
    /// Tests that corrupted event log data is detected and throws.
    ///
    /// NOTE: Prior to hash-chain validation (Phase 2-3), this test expected the materializer
    /// to gracefully fall back to a snapshot when event data was corrupted. Now that we have
    /// CTU hash-chain validation, corrupted data throws immediately - which is the correct
    /// and more secure behavior.
    /// </summary>
    [Fact]
    public async Task SnapshottingMaterializer_ThrowsOnCorruptedEventLog()
    {
        var stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0");

        var plateId1 = new PlateId(Guid.NewGuid());
        var plateId2 = new PlateId(Guid.NewGuid());
        var boundaryId = new BoundaryId(Guid.NewGuid());

        var events = new List<IPlateTopologyEvent>
        {
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId1, new CanonicalTick(0), 0, stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), plateId2, new CanonicalTick(1), 1, stream),
            TestEventFactory.BoundaryCreated(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new Segment2(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                stream)
        };

        var (_, kv) = TestStores.CreateEventStoreWithKv();

        // Write events but do NOT materialize (no snapshot created)
        using (var store1 = new PlateTopologyEventStore(kv))
        {
            await store1.AppendAsync(stream, events, CancellationToken.None);
        }

        // Corrupt the event record
        {
            var prefix = Encoding.UTF8.GetBytes($"S:{stream.VariantId}:{stream.BranchId}:L{stream.LLevel}:{stream.Domain}:M{stream.Model}:");
            var key = BuildEventKey(prefix, 2);

            if (!kv.TryGet(key, out var recordBytes) || recordBytes.Length == 0)
                throw new InvalidOperationException("Expected event record bytes to exist for sequence 2");

            recordBytes[^1] ^= 0xFF;
            kv.Put(key, recordBytes);
        }

        // Now reading the corrupted data should throw hash mismatch
        // Since there's no snapshot, materializer must read all events including the corrupted one
        using (var store2 = new PlateTopologyEventStore(kv))
        {
            var snapshotting2 = new SnapshottingPlateTopologyMaterializer(store2, store2);

            // Hash validation should detect corruption and throw
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => snapshotting2.MaterializeAtSequenceAsync(stream, 2, CancellationToken.None));

            Assert.Contains("hash mismatch", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
     }

    private static byte[] BuildEventKey(byte[] prefix, long sequence)
    {
        var key = new byte[prefix.Length + 2 + 8];
        Buffer.BlockCopy(prefix, 0, key, 0, prefix.Length);

        key[prefix.Length] = (byte)'E';
        key[prefix.Length + 1] = (byte)':';

        BinaryPrimitives.WriteUInt64BigEndian(key.AsSpan(prefix.Length + 2), (ulong)sequence);

        return key;
    }
}
