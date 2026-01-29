using FantaSim.Geosphere.Plate.Topology.Materializer;
using FantaSim.Geosphere.Plate.Testing.Storage;

namespace FantaSim.Geosphere.Plate.Topology.Tests.Integration;

internal static class TestStores
{
    public static PlateTopologyEventStore CreateEventStore()
    {
        return new PlateTopologyEventStore(new InMemoryOrderedKeyValueStore());
    }

    public static (PlateTopologyEventStore Store, InMemoryOrderedKeyValueStore Kv) CreateEventStoreWithKv()
    {
        var kv = new InMemoryOrderedKeyValueStore();
        var store = new PlateTopologyEventStore(kv);
        return (store, kv);
    }
}
