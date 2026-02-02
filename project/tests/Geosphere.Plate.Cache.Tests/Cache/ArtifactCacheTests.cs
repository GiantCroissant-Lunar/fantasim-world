using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Cache;
using FantaSim.Geosphere.Plate.Cache.Materializer.Hashing;
using FantaSim.Geosphere.Plate.Cache.Materializer.Storage;
using FantaSim.Geosphere.Plate.Testing.Storage;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Tests.Cache;

public class ArtifactCacheTests
{
    [Fact]
    public async Task GetOrCreateArtifactAsync_ReturnsHit_WhenManifestAndPayloadMatch()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var cache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var parameters = new Dictionary<string, object> { ["compression"] = "lz4" };
        var paramsHash = ParamsHashComputer.Compute(parameters);
        var inputFingerprint = InputFingerprintComputer.Compute(
            stream.ToEventStreamIdString(),
            "sequence",
            10,
            "TestGen",
            "1.0.0",
            paramsHash);

        var manifestKey = KeyBuilder.BuildManifestKey(stream, "Atlas", inputFingerprint);
        var payloadKey = KeyBuilder.BuildPayloadKey(stream, "Atlas", inputFingerprint);
        var payload = Encoding.UTF8.GetBytes("cached-payload");

        var manifest = Manifest.Create(
            "Atlas",
            inputFingerprint,
            stream.ToString(),
            Boundary.Sequence(10),
            new GeneratorInfo("TestGen", "1.0.0"),
            paramsHash,
            StorageInfo.Embedded(ParamsHashComputer.EmptyParamsHash, 0)) with
        {
            Params = parameters
        };

        await storage.StoreAsync(manifestKey, manifest, payloadKey, payload, CancellationToken.None);

        var result = await cache.GetOrCreateArtifactAsync(
            stream,
            "Atlas",
            10,
            "TestGen",
            "1.0.0",
            parameters,
            new PassthroughGenerator(),
            CancellationToken.None);

        Assert.True(result.IsHit);
        Assert.NotNull(result.Payload);
        Assert.Equal(payload, result.Payload!);
        Assert.NotNull(result.Manifest);
        Assert.Equal(inputFingerprint, result.InputFingerprint);
    }

    [Fact]
    public async Task GetOrCreateArtifactAsync_ReturnsMiss_WhenManifestMissing()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var cache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var parameters = new Dictionary<string, object>();

        var result = await cache.GetOrCreateArtifactAsync(
            stream,
            "Atlas",
            0,
            "TestGen",
            "1.0.0",
            parameters,
            new PassthroughGenerator(),
            CancellationToken.None);

        Assert.False(result.IsHit);
        Assert.NotNull(result.InputFingerprint);
        Assert.Null(result.Payload);
        Assert.Null(result.Manifest);
    }

    [Fact]
    public async Task GetOrCreateArtifactAsync_Throws_WhenPayloadHashMismatch()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var cache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var parameters = new Dictionary<string, object>();
        var paramsHash = ParamsHashComputer.Compute(parameters);
        var inputFingerprint = InputFingerprintComputer.Compute(
            stream.ToEventStreamIdString(),
            "sequence",
            1,
            "TestGen",
            "1.0.0",
            paramsHash);

        var manifestKey = KeyBuilder.BuildManifestKey(stream, "Atlas", inputFingerprint);
        var payloadKey = KeyBuilder.BuildPayloadKey(stream, "Atlas", inputFingerprint);
        var payload = Encoding.UTF8.GetBytes("cached-payload");

        var manifest = Manifest.Create(
            "Atlas",
            inputFingerprint,
            stream.ToString(),
            Boundary.Sequence(1),
            new GeneratorInfo("TestGen", "1.0.0"),
            paramsHash,
            StorageInfo.Embedded(ParamsHashComputer.EmptyParamsHash, 0));

        await storage.StoreAsync(manifestKey, manifest, payloadKey, payload, CancellationToken.None);

        store.Put(Encoding.UTF8.GetBytes(payloadKey), Encoding.UTF8.GetBytes("tampered"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cache.GetOrCreateArtifactAsync(
                stream,
                "Atlas",
                1,
                "TestGen",
                "1.0.0",
                parameters,
                new PassthroughGenerator(),
                CancellationToken.None));
    }

    private sealed class PassthroughGenerator : IArtifactGenerator<byte[]>
    {
        public string GeneratorId => "TestGen";

        public string GeneratorVersion => "1.0.0";

        public Task<byte[]> GenerateAsync(ArtifactGenerationContext context, CancellationToken ct) =>
            Task.FromResult(Array.Empty<byte>());

        public byte[] Serialize(byte[] artifact) => artifact;

        public byte[] Deserialize(byte[] data) => data;
    }
}
