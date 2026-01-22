using System.Buffers.Binary;
using System.Linq;
using System.Text;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;
using RocksDb.Managed;

namespace Plate.Topology.Tests.Integration;

public sealed class SnapshottingMaterializerTests : IDisposable
{
    private const string TestDbPath = "./test_db_snapshotting_materializer";

    public SnapshottingMaterializerTests()
    {
        if (Directory.Exists(TestDbPath))
            Directory.Delete(TestDbPath, true);
    }

    public void Dispose()
    {
        if (Directory.Exists(TestDbPath))
            Directory.Delete(TestDbPath, true);
    }

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
            new PlateCreatedEvent(Guid.NewGuid(), plateId1, DateTimeOffset.UtcNow, 0, stream),
            new PlateCreatedEvent(Guid.NewGuid(), plateId2, DateTimeOffset.UtcNow, 1, stream),
            new BoundaryCreatedEvent(
                Guid.NewGuid(),
                boundaryId,
                plateId1,
                plateId2,
                BoundaryType.Transform,
                new LineSegment(0.0, 0.0, 1.0, 0.0),
                DateTimeOffset.UtcNow,
                2,
                stream)
        };

        using (var store = new PlateTopologyEventStore(TestDbPath))
        {
            await store.AppendAsync(stream, events, CancellationToken.None);

            var snapshotting = new SnapshottingPlateTopologyMaterializer(store, store);
            var r1 = await snapshotting.MaterializeAtTickAsync(stream, 2, CancellationToken.None);

            Assert.False(r1.FromSnapshot);
            Assert.Equal(2, r1.State.LastEventSequence);
            Assert.Equal(2, r1.State.Plates.Count);
            Assert.Single(r1.State.Boundaries);
        }

        using (var db = Db.Open(TestDbPath))
        {
            var prefix = Encoding.UTF8.GetBytes($"S:{stream.VariantId}:{stream.BranchId}:L{stream.LLevel}:{stream.Domain}:M{stream.Model}:");
            var key = BuildEventKey(prefix, 2);
            var recordBytes = db.Get(key);

            if (recordBytes == null || recordBytes.Length == 0)
                throw new InvalidOperationException("Expected event record bytes to exist for sequence 2");

            recordBytes[^1] ^= 0xFF;
            db.Put(key, recordBytes);
        }

        using (var store = new PlateTopologyEventStore(TestDbPath))
        {
            var snapshotting = new SnapshottingPlateTopologyMaterializer(store, store);
            var r2 = await snapshotting.MaterializeAtTickAsync(stream, 2, CancellationToken.None);

            Assert.True(r2.FromSnapshot);
            Assert.Equal(2, r2.State.LastEventSequence);
            Assert.Equal(2, r2.State.Plates.Count);
            Assert.Single(r2.State.Boundaries);
            Assert.Contains(plateId1, r2.State.Plates.Keys);
            Assert.Contains(plateId2, r2.State.Plates.Keys);
            Assert.Contains(boundaryId, r2.State.Boundaries.Keys);

            var r3 = await snapshotting.MaterializeAtTickAsync(stream, 100, CancellationToken.None);
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
