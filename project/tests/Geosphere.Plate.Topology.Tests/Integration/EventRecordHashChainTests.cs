using System.Buffers.Binary;
using System.Linq;
using System.Text;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Events;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Materializer;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Integration;

public sealed class EventRecordHashChainTests
{
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
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(0), 0, stream),
            TestEventFactory.PlateCreated(Guid.NewGuid(), new PlateId(Guid.NewGuid()), new CanonicalTick(1), 1, stream)
        };

        var (store, kv) = TestStores.CreateEventStoreWithKv();
        using (store)
        {
            await store.AppendAsync(stream, events, CancellationToken.None);

            var prefix = Encoding.UTF8.GetBytes($"S:{stream.VariantId}:{stream.BranchId}:L{stream.LLevel}:{stream.Domain}:M{stream.Model}:");
            var key = BuildEventKey(prefix, 1);

            if (!kv.TryGet(key, out var recordBytes) || recordBytes.Length == 0)
                throw new InvalidOperationException("Expected event record bytes to exist for sequence 1");

            recordBytes[^1] ^= 0xFF;
            kv.Put(key, recordBytes);

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
