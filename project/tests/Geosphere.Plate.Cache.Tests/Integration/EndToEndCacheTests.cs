using System.Security.Cryptography;
using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Cache;
using FantaSim.Geosphere.Plate.Cache.Materializer.Hashing;
using FantaSim.Geosphere.Plate.Cache.Materializer.Storage;
using FantaSim.Geosphere.Plate.Cache.Tests.TestHelpers;
using FantaSim.Geosphere.Plate.Testing.Storage;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Tests.Integration;

public class EndToEndCacheTests
{
    [Fact]
    public async Task CacheMiss_GenerateAndStore()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var cache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var generator = new FakeArtifactGenerator();

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        var miss = await cache.GetOrCreateArtifactAsync(
            stream,
            "Atlas",
            10,
            generator.GeneratorId,
            generator.GeneratorVersion,
            parameters,
            generator,
            CancellationToken.None);

        Assert.False(miss.IsHit);

        var payload = await generator.GenerateAsync(new ArtifactGenerationContext(stream, 10, miss.InputFingerprint!), CancellationToken.None);
        await cache.StoreArtifactAsync(
            stream,
            "Atlas",
            10,
            miss.InputFingerprint!,
            generator.GeneratorId,
            generator.GeneratorVersion,
            parameters,
            payload,
            CancellationToken.None);

        var hit = await cache.GetOrCreateArtifactAsync(
            stream,
            "Atlas",
            10,
            generator.GeneratorId,
            generator.GeneratorVersion,
            parameters,
            generator,
            CancellationToken.None);

        Assert.True(hit.IsHit);
        Assert.Equal(payload, hit.Payload);
    }

    [Fact]
    public async Task CacheHit_ReturnCached()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var cache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var generator = new FakeArtifactGenerator();

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        var paramsHash = ParamsHashComputer.Compute(parameters);
        var inputFingerprint = InputFingerprintComputer.Compute(
            stream.ToEventStreamIdString(),
            "sequence",
            2,
            generator.GeneratorId,
            generator.GeneratorVersion,
            paramsHash);

        var payload = await generator.GenerateAsync(new ArtifactGenerationContext(stream, 2, inputFingerprint), CancellationToken.None);
        await cache.StoreArtifactAsync(
            stream,
            "Atlas",
            2,
            inputFingerprint,
            generator.GeneratorId,
            generator.GeneratorVersion,
            parameters,
            payload,
            CancellationToken.None);

        var hit = await cache.GetOrCreateArtifactAsync(
            stream,
            "Atlas",
            2,
            generator.GeneratorId,
            generator.GeneratorVersion,
            parameters,
            generator,
            CancellationToken.None);

        Assert.True(hit.IsHit);
        Assert.Equal(payload, hit.Payload);
    }

    [Fact]
    public async Task ContentVerification_FailsOnCorruption()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var cache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var generator = new FakeArtifactGenerator();

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        var paramsHash = ParamsHashComputer.Compute(parameters);
        var inputFingerprint = InputFingerprintComputer.Compute(
            stream.ToEventStreamIdString(),
            "sequence",
            1,
            generator.GeneratorId,
            generator.GeneratorVersion,
            paramsHash);
        var payload = Encoding.UTF8.GetBytes("payload");
        var manifest = Manifest.Create(
            "Atlas",
            inputFingerprint,
            stream.ToString(),
            Boundary.Sequence(1),
            new GeneratorInfo(generator.GeneratorId, generator.GeneratorVersion),
            paramsHash,
            StorageInfo.Embedded(ParamsHashComputer.EmptyParamsHash, 0));

        var manifestKey = KeyBuilder.BuildManifestKey(stream, "Atlas", inputFingerprint);
        var payloadKey = KeyBuilder.BuildPayloadKey(stream, "Atlas", inputFingerprint);
        await storage.StoreAsync(manifestKey, manifest, payloadKey, payload, CancellationToken.None);

        store.Put(Encoding.UTF8.GetBytes(payloadKey), Encoding.UTF8.GetBytes("tampered"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cache.GetOrCreateArtifactAsync(
                stream,
                "Atlas",
                1,
                generator.GeneratorId,
                generator.GeneratorVersion,
                parameters,
                generator,
                CancellationToken.None));
    }

    [Fact]
    public async Task FingerprintMismatch_Detected()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var cache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var generator = new FakeArtifactGenerator();

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        var paramsHash = ParamsHashComputer.Compute(parameters);

        var inputFingerprint = InputFingerprintComputer.Compute(
            stream.ToEventStreamIdString(),
            "sequence",
            3,
            generator.GeneratorId,
            generator.GeneratorVersion,
            paramsHash);

        var payload = Encoding.UTF8.GetBytes("payload");
        var manifest = Manifest.Create(
            "Atlas",
            new string('a', 64),
            stream.ToString(),
            Boundary.Sequence(3),
            new GeneratorInfo(generator.GeneratorId, generator.GeneratorVersion),
            paramsHash,
            StorageInfo.Embedded(ParamsHashComputer.EmptyParamsHash, 0));

        var manifestKey = KeyBuilder.BuildManifestKey(stream, "Atlas", inputFingerprint);
        var payloadKey = KeyBuilder.BuildPayloadKey(stream, "Atlas", inputFingerprint);
        await storage.StoreAsync(manifestKey, manifest, payloadKey, payload, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cache.GetOrCreateArtifactAsync(
                stream,
                "Atlas",
                3,
                generator.GeneratorId,
                generator.GeneratorVersion,
                parameters,
                generator,
                CancellationToken.None));
    }

    [Fact]
    public async Task ExternalStorage_RoundTrip()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var cache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var generator = new FakeArtifactGenerator();
        var externalBackend = new InMemoryExternalStorageBackend();

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        var paramsHash = ParamsHashComputer.Compute(parameters);
        var inputFingerprint = InputFingerprintComputer.Compute(
            stream.ToEventStreamIdString(),
            "sequence",
            4,
            generator.GeneratorId,
            generator.GeneratorVersion,
            paramsHash);

        var payload = Encoding.UTF8.GetBytes("external-payload");
        var uri = externalBackend.Put(payload);
        var contentHash = ComputeSha256Hex(payload);
        var manifest = Manifest.Create(
            "Atlas",
            inputFingerprint,
            stream.ToString(),
            Boundary.Sequence(4),
            new GeneratorInfo(generator.GeneratorId, generator.GeneratorVersion),
            paramsHash,
            StorageInfo.External(contentHash, (ulong)payload.Length)) with
        {
            External = new ExternalStorageInfo(uri, null, "memory")
        };

        var manifestKey = KeyBuilder.BuildManifestKey(stream, "Atlas", inputFingerprint);
        var payloadKey = KeyBuilder.BuildPayloadKey(stream, "Atlas", inputFingerprint);
        await storage.StoreAsync(manifestKey, manifest, payloadKey, payload, CancellationToken.None);

        var loadedManifest = await storage.GetManifestAsync(manifestKey, CancellationToken.None);
        Assert.NotNull(loadedManifest);
        var externalPayload = await externalBackend.GetAsync(uri, CancellationToken.None);

        Assert.NotNull(externalPayload);
        Assert.Equal(payload, externalPayload);
    }

    [Fact]
    public async Task Enumeration_ReturnsAllArtifacts()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var cache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var generator = new FakeArtifactGenerator();
        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        for (var i = 0; i < 3; i++)
        {
            var inputFingerprint = InputFingerprintComputer.Compute(
                stream.ToEventStreamIdString(),
                "sequence",
                (ulong)i,
                generator.GeneratorId,
                generator.GeneratorVersion,
                ParamsHashComputer.EmptyParamsHash);

            var payload = Encoding.UTF8.GetBytes($"payload-{i}");
            await cache.StoreArtifactAsync(
                stream,
                "Atlas",
                i,
                inputFingerprint,
                generator.GeneratorId,
                generator.GeneratorVersion,
                parameters,
                payload,
                CancellationToken.None);
        }

        var prefix = KeyBuilder.BuildPrefixForEnumeration(stream, "Atlas");
        var keys = new List<string>();
        await foreach (var key in storage.EnumerateKeysAsync(prefix, CancellationToken.None))
        {
            keys.Add(key);
        }

        Assert.Equal(6, keys.Count);
        Assert.Equal(3, keys.Count(k => k.EndsWith(":Manifest", StringComparison.Ordinal)));
        Assert.Equal(3, keys.Count(k => k.EndsWith(":Payload", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task GarbageCollector_RemovesOldArtifacts()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var cache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var generator = new FakeArtifactGenerator();
        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        for (var i = 0; i < 3; i++)
        {
            var inputFingerprint = InputFingerprintComputer.Compute(
                stream.ToEventStreamIdString(),
                "sequence",
                (ulong)i,
                generator.GeneratorId,
                generator.GeneratorVersion,
                ParamsHashComputer.EmptyParamsHash);

            var payload = Encoding.UTF8.GetBytes($"payload-{i}");
            await cache.StoreArtifactAsync(
                stream,
                "Atlas",
                i,
                inputFingerprint,
                generator.GeneratorId,
                generator.GeneratorVersion,
                parameters,
                payload,
                CancellationToken.None);
        }

        var gc = new GarbageCollector(storage);
        var retention = new RetentionPolicy(MaxSequenceAge: 1, MinArtifactsToKeep: 1);
        var prefix = KeyBuilder.BuildPrefixForEnumeration(stream, "Atlas");

        await gc.CollectAsync(prefix, retention, CancellationToken.None);

        var remainingKeys = new List<string>();
        await foreach (var key in storage.EnumerateKeysAsync(prefix, CancellationToken.None))
        {
            remainingKeys.Add(key);
        }

        Assert.True(remainingKeys.Count < 6);
    }

    private static string ComputeSha256Hex(byte[] payload)
    {
        var hashBytes = SHA256.HashData(payload);
        var sb = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }
        return sb.ToString();
    }
}
