using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Cache;
using FantaSim.Geosphere.Plate.Cache.Materializer.Storage;
using FantaSim.Geosphere.Plate.Testing.Storage;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Tests.DerivedProduct;

public sealed class DerivedProductInvalidationTests
{
    [Fact]
    public async Task InvalidateOnTopologyChange_RemovesFromL1_AllowsL2Hit()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var artifactCache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var cache = new DerivedProductCache(artifactCache);

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var key = new DerivedProductKey(stream, "TestProduct", 10, "policy-hash-123");
        var generator = new CountingGenerator();

        var result1 = await cache.GetOrComputeAsync(key, generator, CancellationToken.None);
        Assert.False(result1.IsHit);
        Assert.Equal(1, generator.ComputeCount);

        var result2 = await cache.GetOrComputeAsync(key, generator, CancellationToken.None);
        Assert.True(result2.IsHit);
        Assert.Equal(1, generator.ComputeCount);

        cache.InvalidateOnTopologyChange(stream.ToStreamKey());

        var result3 = await cache.GetOrComputeAsync(key, generator, CancellationToken.None);
        Assert.True(result3.IsHit);
        Assert.Equal(1, generator.ComputeCount);
        Assert.True(cache.Metrics.InvalidationCount > 0);
    }

    [Fact]
    public async Task InvalidateOnKinematicsChange_RemovesFromL1_AllowsL2Hit()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var artifactCache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var cache = new DerivedProductCache(artifactCache);

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var key = new DerivedProductKey(stream, "TestProduct", 10, "M0");
        var generator = new CountingGenerator();

        await cache.GetOrComputeAsync(key, generator, CancellationToken.None);

        cache.InvalidateOnKinematicsChange("M0");

        var result = await cache.GetOrComputeAsync(key, generator, CancellationToken.None);
        Assert.True(result.IsHit);
        Assert.Equal(1, generator.ComputeCount);
        Assert.True(cache.Metrics.InvalidationCount > 0);
    }

    [Fact]
    public async Task Invalidate_SpecificInstance_RemovesOnlyThatInstance()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var artifactCache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var cache = new DerivedProductCache(artifactCache);

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var key1 = new DerivedProductKey(stream, "TestProduct", 10, "policy-1");
        var key2 = new DerivedProductKey(stream, "TestProduct", 10, "policy-2");
        var generator = new CountingGenerator();

        var result1 = await cache.GetOrComputeAsync(key1, generator, CancellationToken.None);
        await cache.GetOrComputeAsync(key2, generator, CancellationToken.None);
        Assert.Equal(2, generator.ComputeCount);

        cache.Invalidate(result1.ProductInstanceId!);

        var result2Again = await cache.GetOrComputeAsync(key2, generator, CancellationToken.None);
        Assert.True(result2Again.IsHit);
        Assert.Equal(2, generator.ComputeCount);
    }

    [Fact]
    public async Task Clear_RemovesAllEntries()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var artifactCache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var cache = new DerivedProductCache(artifactCache);

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var key1 = new DerivedProductKey(stream, "TestProduct", 10, "policy-1");
        var key2 = new DerivedProductKey(stream, "TestProduct", 10, "policy-2");
        var generator = new CountingGenerator();

        await cache.GetOrComputeAsync(key1, generator, CancellationToken.None);
        await cache.GetOrComputeAsync(key2, generator, CancellationToken.None);

        cache.Clear();

        var result = await cache.GetOrComputeAsync(key2, generator, CancellationToken.None);
        Assert.True(result.IsHit);
        Assert.Equal(2, generator.ComputeCount);
        Assert.True(cache.Metrics.InvalidationCount > 0);
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
