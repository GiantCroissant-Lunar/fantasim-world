using System.Text;
using FantaSim.Geosphere.Plate.Cache.Contracts;
using FantaSim.Geosphere.Plate.Cache.Contracts.Models;
using FantaSim.Geosphere.Plate.Cache.Materializer.Cache;
using FantaSim.Geosphere.Plate.Cache.Materializer.Storage;
using FantaSim.Geosphere.Plate.Testing.Storage;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;

namespace FantaSim.Geosphere.Plate.Cache.Tests.DerivedProduct;

public sealed class DerivedProductNoPersistenceTests
{
    [Fact]
    public void DerivedProductProvenance_HasDisclaimer()
    {
        var provenance = new DerivedProductProvenance
        {
            ProductInstanceId = "test-instance",
            ProductType = "TestProduct",
            SourceTruthHashes = new[] { "hash1", "hash2" },
            PolicyHash = "policy-hash",
            GeneratorId = "TestGen",
            GeneratorVersion = "1.0.0",
            ComputedAt = DateTimeOffset.UtcNow,
            ComputationTimeMs = 100
        };

        Assert.Equal(DerivedProductLabels.DerivedProductNotTruth, provenance.Disclaimer);
    }

    [Fact]
    public async Task DerivedProductCache_DoesNotStoreTruthEventKeys()
    {
        var store = new InMemoryOrderedKeyValueStore();
        var storage = new EmbeddedStorage(store);
        var artifactCache = new ArtifactCache(storage, new ArtifactCacheOptions(StorageMode.Embedded));
        var cache = new DerivedProductCache(artifactCache);

        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var key = new DerivedProductKey(stream, "TestProduct", 10, "policy-hash-123");
        var generator = new TestGenerator();

        await cache.GetOrComputeAsync(key, generator, CancellationToken.None);
        await cache.GetOrComputeAsync(key, generator, CancellationToken.None);
        cache.InvalidateOnTopologyChange(stream.ToStreamKey());
        cache.Clear();

        await foreach (var k in storage.EnumerateKeysAsync("S:", CancellationToken.None))
        {
            Assert.DoesNotContain(":Events", k, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("truth", k, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(":Derived:", k, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DerivedProductLookupResult_DistinguishesCacheHitFromComputed()
    {
        var provenance = new DerivedProductProvenance
        {
            ProductInstanceId = "test-instance",
            ProductType = "TestProduct",
            SourceTruthHashes = new[] { "hash1" },
            PolicyHash = "policy-hash",
            GeneratorId = "TestGen",
            GeneratorVersion = "1.0.0",
            ComputedAt = DateTimeOffset.UtcNow,
            ComputationTimeMs = 50
        };

        var hitResult = DerivedProductLookupResult<string>.Hit("value", provenance, "instance-1");
        var computedResult = DerivedProductLookupResult<string>.Computed("value", provenance, "instance-2");

        Assert.True(hitResult.IsHit);
        Assert.False(computedResult.IsHit);

        Assert.NotNull(hitResult.Provenance);
        Assert.NotNull(computedResult.Provenance);

        Assert.Equal(DerivedProductLabels.DerivedProductNotTruth, hitResult.Provenance!.Value.Disclaimer);
        Assert.Equal(DerivedProductLabels.DerivedProductNotTruth, computedResult.Provenance!.Value.Disclaimer);
    }

    [Fact]
    public void DerivedProductKey_GeneratesDeterministicInstanceId()
    {
        var stream = new TruthStreamIdentity("V1", "main", 0, Domain.Parse("geo.plates"), "M0");
        var key1 = new DerivedProductKey(stream, "TestProduct", 10, "policy-hash");
        var key2 = new DerivedProductKey(stream, "TestProduct", 10, "policy-hash");
        var key3 = new DerivedProductKey(stream, "TestProduct", 11, "policy-hash");

        var id1 = key1.ToInstanceId();
        var id2 = key2.ToInstanceId();
        var id3 = key3.ToInstanceId();

        Assert.Equal(id1, id2);
        Assert.NotEqual(id1, id3);
    }

    private sealed class TestGenerator : IDerivedProductGenerator<string>
    {
        public string GeneratorId => "TestGen";

        public string GeneratorVersion => "1.0.0";

        public Task<string> ComputeAsync(CancellationToken ct) => Task.FromResult("test-value");

        public byte[] Serialize(string product) => Encoding.UTF8.GetBytes(product);

        public string Deserialize(byte[] data) => Encoding.UTF8.GetString(data);
    }
}
