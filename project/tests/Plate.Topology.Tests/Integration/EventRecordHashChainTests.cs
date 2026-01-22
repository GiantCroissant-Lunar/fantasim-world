using System.Buffers.Binary;
using System.Linq;
using System.Text;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;
using Plate.Topology.Materializer;
using RocksDb.Managed;

namespace Plate.Topology.Tests.Integration;

public sealed class EventRecordHashChainTests : IDisposable
{
    private const string TestDbPath = "./test_db_event_record_hash_chain";

    public EventRecordHashChainTests()
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
    public async Task ReadAsync_TamperedRecord_ThrowsInvalidOperationException()
    {
        var stream = new TruthStreamIdentity(
            "science",
            "main",
            2,
            Domain.Parse("geo.plates"),
            "0");

        var events = new List<IPlateTopologyEvent>
        {
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 0, stream),
            new PlateCreatedEvent(Guid.NewGuid(), new PlateId(Guid.NewGuid()), DateTimeOffset.UtcNow, 1, stream)
        };

        using (var store = new PlateTopologyEventStore(TestDbPath))
        {
            await store.AppendAsync(stream, events, CancellationToken.None);
        }

        using (var db = Db.Open(TestDbPath))
        {
            var prefix = Encoding.UTF8.GetBytes($"S:{stream.VariantId}:{stream.BranchId}:L{stream.LLevel}:{stream.Domain}:M{stream.Model}:");
            var key = BuildEventKey(prefix, 1);
            var recordBytes = db.Get(key);

            if (recordBytes == null || recordBytes.Length == 0)
                throw new InvalidOperationException("Expected event record bytes to exist for sequence 1");

            recordBytes[^1] ^= 0xFF;
            db.Put(key, recordBytes);
        }

        using (var store = new PlateTopologyEventStore(TestDbPath))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await store.ReadAsync(stream, 0, CancellationToken.None).ToListAsync());
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
