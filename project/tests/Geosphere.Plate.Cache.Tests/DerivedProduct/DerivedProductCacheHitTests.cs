using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Cache;
using FantaSim.Geosphere.Plate.Cache.Materializer.Storage;
using FantaSim.Geosphere.Plate.Testing.Storage;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Tests.DerivedProduct;

public sealed class DerivedProductCacheHitTests
{
    [Fact]
    public async Task SameInputs_ReturnsSameInstance()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var artifactCache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var cache = new DerivedProductCache(artifactCache);

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var key = new DerivedProductKey(stream, "TestProduct", 10, "policy-hash-123");
        var generator = new CountingGenerator();

        var result1 = await cache.GetOrComputeAsync(key, generator, CancellationToken.None);
        var result2 = await cache.GetOrComputeAsync(key, generator, CancellationToken.None);

        Assert.False(result1.IsHit);
        Assert.True(result2.IsHit);
        Assert.Equal(1, generator.ComputeCount);
        Assert.Equal(result1.ProductInstanceId, result2.ProductInstanceId);
    }

    [Fact]
    public async Task DifferentInputs_ReturnsDifferentInstances()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var artifactCache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var cache = new DerivedProductCache(artifactCache);

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var key1 = new DerivedProductKey(stream, "TestProduct", 10, "policy-hash-123");
        var key2 = new DerivedProductKey(stream, "TestProduct", 10, "policy-hash-456");
        var generator = new CountingGenerator();

        var result1 = await cache.GetOrComputeAsync(key1, generator, CancellationToken.None);
        var result2 = await cache.GetOrComputeAsync(key2, generator, CancellationToken.None);

        Assert.False(result1.IsHit);
        Assert.False(result2.IsHit);
        Assert.Equal(2, generator.ComputeCount);
        Assert.NotEqual(result1.ProductInstanceId, result2.ProductInstanceId);
    }

    [Fact]
    public async Task CacheHit_ReturnsProvenance()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var artifactCache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var cache = new DerivedProductCache(artifactCache);

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var key = new DerivedProductKey(stream, "TestProduct", 10, "policy-hash-123");
        var generator = new CountingGenerator();

        await cache.GetOrComputeAsync(key, generator, CancellationToken.None);
        var result = await cache.GetOrComputeAsync(key, generator, CancellationToken.None);

        Assert.True(result.IsHit);
        Assert.NotNull(result.Provenance);
        Assert.Equal("TestProduct", result.Provenance!.Value.ProductType);
        Assert.Equal("TestGen", result.Provenance!.Value.GeneratorId);
        Assert.Equal("1.0.0", result.Provenance!.Value.GeneratorVersion);
        Assert.Equal(DerivedProductLabels.DerivedProductNotTruth, result.Provenance!.Value.Disclaimer);
    }

    private sealed class CountingGenerator : IDerivedProductGenerator<string>
    {
        public int ComputeCount { get; private set; }

        public string GeneratorId => "TestGen";

        public string GeneratorVersion => "1.0.0";

        public Task<string> ComputeAsync(CancellationToken ct)
        {
            ComputeCount++;
            return Task.FromResult($"computed-value-{ComputeCount}");
        }

        public byte[] Serialize(string product) => Encoding.UTF8.GetBytes(product);

        public string Deserialize(byte[] data) => Encoding.UTF8.GetString(data);
    }
}
