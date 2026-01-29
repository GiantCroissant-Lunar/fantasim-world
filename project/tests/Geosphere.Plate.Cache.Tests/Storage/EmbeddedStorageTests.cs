using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Hashing;
using FantaSim.Geosphere.Plate.Cache.Materializer.Storage;
using FantaSim.Geosphere.Plate.Testing.Storage;

namespace FantaSim.Geosphere.Plate.Cache.Tests.Storage;

public class EmbeddedStorageTests
{
    [Fact]
    public async Task StoreAndLoad_ReturnsManifestAndPayload()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);

        var payload = Encoding.UTF8.GetBytes("payload-data");
        var manifest = CreateManifest("PlateTopologySnapshot");
        var manifestKey = "S:V1:main:L0:geo.plates:M0:Derived:PlateTopologySnapshot:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa:Manifest";
        var payloadKey = "S:V1:main:L0:geo.plates:M0:Derived:PlateTopologySnapshot:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa:Payload";

        await storage.StoreAsync(manifestKey, manifest, payloadKey, payload, CancellationToken.None);

        var loadedManifest = await storage.GetManifestAsync(manifestKey, CancellationToken.None);
        Assert.NotNull(loadedManifest);
        Assert.Equal(StorageMode.Embedded, loadedManifest!.Value.Storage.Mode);
        Assert.Equal((ulong)payload.Length, loadedManifest.Value.Storage.ContentLength);
        Assert.Equal(ComputeSha256Hex(payload), loadedManifest.Value.Storage.ContentHash);

        var loadedPayload = await storage.GetPayloadAsync(payloadKey, CancellationToken.None);
        Assert.NotNull(loadedPayload);
        Assert.Equal(payload, loadedPayload!);

        var prefix = "S:V1:main:L0:geo.plates:M0:Derived:PlateTopologySnapshot:";
        var keys = new List<string>();
        await foreach (var key in storage.EnumerateKeysAsync(prefix, CancellationToken.None))
        {
            keys.Add(key);
        }

        Assert.Contains(manifestKey, keys);
        Assert.Contains(payloadKey, keys);
    }

    [Fact]
    public async Task GetPayloadAsync_ThrowsWhenHashMismatch()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);

        var payload = Encoding.UTF8.GetBytes("payload-data");
        var manifest = CreateManifest("PlateTopologySnapshot");
        var manifestKey = "S:V1:main:L0:geo.plates:M0:Derived:PlateTopologySnapshot:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa:Manifest";
        var payloadKey = "S:V1:main:L0:geo.plates:M0:Derived:PlateTopologySnapshot:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa:Payload";

        await storage.StoreAsync(manifestKey, manifest, payloadKey, payload, CancellationToken.None);

        store.Put(Encoding.UTF8.GetBytes(payloadKey), Encoding.UTF8.GetBytes("tampered"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.GetPayloadAsync(payloadKey, CancellationToken.None));
    }

    private static Manifest CreateManifest(string productType)
    {
        var inputFingerprint = new string('a', 64);
        var sourceStream = "S:V1:Bmain:L0:Plates:M0:Events";
        var boundary = Boundary.Sequence(10);
        var generator = new GeneratorInfo("TestGen", "1.0.0");
        var paramsHash = ParamsHashComputer.EmptyParamsHash;
        var storageInfo = StorageInfo.Embedded(ParamsHashComputer.EmptyParamsHash, 0);

        return Manifest.Create(productType, inputFingerprint, sourceStream, boundary, generator, paramsHash, storageInfo);
    }

    private static string ComputeSha256Hex(byte[] bytes)
    {
        var hashBytes = SHA256.HashData(bytes);
        var sb = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }
        return sb.ToString();
    }
}
