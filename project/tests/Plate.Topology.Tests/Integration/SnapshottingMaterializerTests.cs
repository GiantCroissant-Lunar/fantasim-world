using System.Buffers.Binary;
using System.Linq;
using System.Text;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;

namespace Plate.Topology.Tests.Integration;

public sealed class SnapshottingMaterializerTests
{
    [Fact]
    public async Task SnapshottingMaterializer_UsesSnapshot_IfEventLogCorrupted()
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
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                new CanonicalTick(2),
                2,
                stream)
        };

        var (_, kv) = TestStores.CreateEventStoreWithKv();

        using (var store1 = new PlateTopologyEventStore(kv))
        {
            await store1.AppendAsync(stream, events, CancellationToken.None);

            var snapshotting1 = new SnapshottingPlateTopologyMaterializer(store1, store1);
            var r1 = await snapshotting1.MaterializeAtTickAsync(stream, 2, CancellationToken.None);

            Assert.False(r1.FromSnapshot);
            Assert.Equal(2, r1.State.LastEventSequence);
            Assert.Equal(2, r1.State.Plates.Count);
            Assert.Single(r1.State.Boundaries);
        }

        {
            var prefix = Encoding.UTF8.GetBytes($"S:{stream.VariantId}:{stream.BranchId}:L{stream.LLevel}:{stream.Domain}:M{stream.Model}:");
            var key = BuildEventKey(prefix, 2);

            if (!kv.TryGet(key, out var recordBytes) || recordBytes.Length == 0)
                throw new InvalidOperationException("Expected event record bytes to exist for sequence 2");

            recordBytes[^1] ^= 0xFF;
            kv.Put(key, recordBytes);
        }

        using (var store2 = new PlateTopologyEventStore(kv))
        {
            var snapshotting2 = new SnapshottingPlateTopologyMaterializer(store2, store2);
            var r2 = await snapshotting2.MaterializeAtTickAsync(stream, 2, CancellationToken.None);

            Assert.True(r2.FromSnapshot);
            Assert.Equal(2, r2.State.LastEventSequence);
            Assert.Equal(2, r2.State.Plates.Count);
            Assert.Single(r2.State.Boundaries);
            Assert.Contains(plateId1, r2.State.Plates.Keys);
            Assert.Contains(plateId2, r2.State.Plates.Keys);
            Assert.Contains(boundaryId, r2.State.Boundaries.Keys);

            var r3 = await snapshotting2.MaterializeAtTickAsync(stream, 100, CancellationToken.None);
            Assert.True(r3.FromSnapshot);
            Assert.Equal(r2.State.LastEventSequence, r3.State.LastEventSequence);
            Assert.Equal(r2.State.Plates.Count, r3.State.Plates.Count);
            Assert.Equal(r2.State.Boundaries.Count, r3.State.Boundaries.Count);
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
