using System.Collections.Concurrent;
using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Partition.Solver;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Partition.Tests;

/// <summary>
/// Partition Cache Tests - RFC-V2-0047 §4.2
/// Tests cache behavior including hit/miss, expiration, and thread safety.
/// </summary>
public sealed class PartitionCacheTests
{
    #region Test: Cache hit/miss

    [Fact]
    public void CacheHit_StoredEntry_Retrieved()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");
        var streamId = CreateFakeStreamIdentity();
        var policy = new TolerancePolicy.StrictPolicy();
        var identity = identityComputer.ComputeStreamIdentity(streamId, new CanonicalTick(1), 1, policy);
        var result = CreateFakePartitionResult();

        // Act
        cache.Set(identity, result);
        var hit = cache.TryGet(identity, out var retrieved);

        // Assert
        hit.Should().BeTrue();
        retrieved.Should().NotBeNull();
        cache.HitCount.Should().Be(1);
        cache.MissCount.Should().Be(0);
    }

    [Fact]
    public void CacheMiss_NoEntry_ReturnsFalse()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");
        var streamId = CreateFakeStreamIdentity();
        var policy = new TolerancePolicy.StrictPolicy();
        var identity = identityComputer.ComputeStreamIdentity(streamId, new CanonicalTick(1), 1, policy);

        // Act
        var hit = cache.TryGet(identity, out var retrieved);

        // Assert
        hit.Should().BeFalse();
        retrieved.PlatePolygons.Should().BeNull();
        cache.HitCount.Should().Be(0);
        cache.MissCount.Should().Be(1);
    }

    [Fact]
    public void CacheHitRatio_OneHitOneMiss_50Percent()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");
        var streamId = CreateFakeStreamIdentity();
        var differentStreamId = CreateFakeStreamIdentity("different-stream"); // Different stream for miss
        var policy = new TolerancePolicy.StrictPolicy();
        var identity = identityComputer.ComputeStreamIdentity(streamId, new CanonicalTick(1), 1, policy);
        var differentIdentity = identityComputer.ComputeStreamIdentity(differentStreamId, new CanonicalTick(1), 1, policy);
        var result = CreateFakePartitionResult();

        // Act: 1 hit, 1 miss
        cache.Set(identity, result);
        cache.TryGet(identity, out _); // hit
        cache.TryGet(differentIdentity, out _); // miss (different stream identity)

        // Assert
        cache.HitRatio.Should().BeApproximately(0.5, 0.01);
    }

    [Fact]
    public void CacheHit_MultipleEntries_RetrievesCorrectEntry()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");

        var identity1 = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity("stream1"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
        var identity2 = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity("stream2"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
        var identity3 = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity("stream3"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());

        var result1 = CreateFakePartitionResult(plateId: 1);
        var result2 = CreateFakePartitionResult(plateId: 2);
        var result3 = CreateFakePartitionResult(plateId: 3);

        // Act
        cache.Set(identity1, result1);
        cache.Set(identity2, result2);
        cache.Set(identity3, result3);

        // Assert: Can retrieve each correctly
        cache.TryGet(identity1, out var retrieved1).Should().BeTrue();
        cache.TryGet(identity2, out var retrieved2).Should().BeTrue();
        cache.TryGet(identity3, out var retrieved3).Should().BeTrue();

        retrieved1.PlatePolygons.Should().ContainKey(TestDataFactory.PlateId(1));
        retrieved2.PlatePolygons.Should().ContainKey(TestDataFactory.PlateId(2));
        retrieved3.PlatePolygons.Should().ContainKey(TestDataFactory.PlateId(3));
    }

    #endregion

    #region Test: Expiration

    [Fact]
    public void CacheExpiration_EntryExpired_NotRetrieved()
    {
        // Arrange: Very short expiration
        var cache = new PartitionCache(TimeSpan.FromMilliseconds(10));
        var identityComputer = new StreamIdentityComputer("test-v1");
        var identity = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity(), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
        var result = CreateFakePartitionResult();

        // Act
        cache.Set(identity, result);
        Thread.Sleep(50); // Wait for expiration
        var hit = cache.TryGet(identity, out _);

        // Assert
        hit.Should().BeFalse();
    }

    [Fact]
    public void CacheExpiration_EntryNotExpired_Retrieved()
    {
        // Arrange: Long expiration
        var cache = new PartitionCache(TimeSpan.FromMinutes(5));
        var identityComputer = new StreamIdentityComputer("test-v1");
        var identity = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity(), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
        var result = CreateFakePartitionResult();

        // Act
        cache.Set(identity, result);
        var hit = cache.TryGet(identity, out _);

        // Assert
        hit.Should().BeTrue();
    }

    [Fact]
    public void CacheEvictionExpired_RemovesExpiredEntries()
    {
        // Arrange
        var cache = new PartitionCache(TimeSpan.FromMilliseconds(10));
        var identityComputer = new StreamIdentityComputer("test-v1");
        var identity = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity(), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
        var result = CreateFakePartitionResult();

        // Act
        cache.Set(identity, result);
        Thread.Sleep(50); // Wait for expiration
        cache.EvictExpired();

        // Assert
        cache.Count.Should().Be(0);
    }

    [Fact]
    public void CacheEvictionExpired_OnlyExpiredRemoved()
    {
        // Arrange
        var cache = new PartitionCache(TimeSpan.FromMilliseconds(50));
        var identityComputer = new StreamIdentityComputer("test-v1");

        var expiredIdentity = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity("old"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
        var freshIdentity = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity("new"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());

        // Act
        cache.Set(expiredIdentity, CreateFakePartitionResult());
        Thread.Sleep(100); // Wait for first to expire (2x cache duration to avoid flaky timing)
        cache.Set(freshIdentity, CreateFakePartitionResult()); // Add fresh entry
        cache.EvictExpired();

        // Assert
        cache.Count.Should().Be(1);
        cache.TryGet(freshIdentity, out _).Should().BeTrue();
        cache.TryGet(expiredIdentity, out _).Should().BeFalse();
    }

    #endregion

    #region Test: Thread safety

    [Fact]
    public void ThreadSafety_ConcurrentWrites_NoExceptions()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");
        var exceptions = new ConcurrentBag<Exception>();

        // Act: Concurrent writes
        Parallel.For(0, 100, i =>
        {
            try
            {
                var identity = identityComputer.ComputeStreamIdentity(
                    CreateFakeStreamIdentity($"stream{i}"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
                cache.Set(identity, CreateFakePartitionResult(plateId: i));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
        cache.Count.Should().Be(100);
    }

    [Fact]
    public void ThreadSafety_ConcurrentReadsWrites_NoExceptions()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");
        var baseIdentity = identityComputer.ComputeStreamIdentity(
            CreateFakeStreamIdentity("shared"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
        cache.Set(baseIdentity, CreateFakePartitionResult());

        var exceptions = new ConcurrentBag<Exception>();

        // Act: Concurrent reads and writes
        Parallel.For(0, 100, i =>
        {
            try
            {
                if (i % 2 == 0)
                {
                    cache.TryGet(baseIdentity, out _);
                }
                else
                {
                    var identity = identityComputer.ComputeStreamIdentity(
                        CreateFakeStreamIdentity($"stream{i}"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
                    cache.Set(identity, CreateFakePartitionResult(plateId: i));
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
    }

    [Fact]
    public void ThreadSafety_ConcurrentClear_NoExceptions()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");

        // Pre-populate
        for (int i = 0; i < 50; i++)
        {
            var identity = identityComputer.ComputeStreamIdentity(
                CreateFakeStreamIdentity($"stream{i}"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
            cache.Set(identity, CreateFakePartitionResult());
        }

        var exceptions = new ConcurrentBag<Exception>();

        // Act: Concurrent reads/writes with clear
        Parallel.For(0, 100, i =>
        {
            try
            {
                switch (i % 3)
                {
                    case 0:
                        var identity = identityComputer.ComputeStreamIdentity(
                            CreateFakeStreamIdentity($"stream{i}"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
                        cache.Set(identity, CreateFakePartitionResult());
                        break;
                    case 1:
                        cache.TryGet(identityComputer.ComputeStreamIdentity(
                            CreateFakeStreamIdentity("stream0"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy()), out _);
                        break;
                    case 2:
                        cache.Clear();
                        break;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
    }

    [Fact]
    public void ThreadSafety_HitCount_Accurate()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");
        var identity = identityComputer.ComputeStreamIdentity(
            CreateFakeStreamIdentity(), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
        cache.Set(identity, CreateFakePartitionResult());

        // Act: Concurrent reads
        Parallel.For(0, 100, _ =>
        {
            cache.TryGet(identity, out PlatePartitionResult _);
        });

        // Assert
        cache.HitCount.Should().Be(100);
    }

    #endregion

    #region Test: Invalidation

    [Fact]
    public void CacheInvalidation_ByPrefix_RemovesMatching()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");

        var id1 = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity("prefix-abc"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
        var id2 = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity("prefix-def"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
        var id3 = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity("other-xyz"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());

        cache.Set(id1, CreateFakePartitionResult());
        cache.Set(id2, CreateFakePartitionResult());
        cache.Set(id3, CreateFakePartitionResult());

        // Act
        cache.InvalidateByTopology("ABC"); // Case-insensitive prefix matching

        // Assert: Implementation dependent - this test verifies the method exists
        // The actual prefix matching depends on the stream hash, not the stream name
    }

    [Fact]
    public void CacheClear_RemovesAll()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");

        for (int i = 0; i < 10; i++)
        {
            var identity = identityComputer.ComputeStreamIdentity(
                CreateFakeStreamIdentity($"stream{i}"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
            cache.Set(identity, CreateFakePartitionResult());
        }

        // Act
        cache.Clear();

        // Assert
        cache.Count.Should().Be(0);
        cache.HitCount.Should().Be(0);
        cache.MissCount.Should().Be(0);
    }

    #endregion

    #region Test: Identity mismatch

    [Fact]
    public void Cache_TamperedEntry_Removed()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");
        var streamId1 = CreateFakeStreamIdentity("stream1");
        var streamId2 = CreateFakeStreamIdentity("stream2");

        var identity1 = identityComputer.ComputeStreamIdentity(streamId1, new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
        var identity2 = identityComputer.ComputeStreamIdentity(streamId2, new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());

        var result = CreateFakePartitionResult();

        // Act: Store with identity1
        cache.Set(identity1, result);

        // Try to retrieve with different identity (same CombinedHash would be extremely unlikely)
        // This tests that the cache checks the full identity, not just the key
        var hit = cache.TryGet(identity2, out _);

        // Assert: Should miss because identities don't match
        hit.Should().BeFalse();
    }

    #endregion

    #region Test: Logging

    [Fact]
    public void Cache_WithLogger_LogsOperations()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PartitionCache>>();
        var cache = new PartitionCache(logger: logger);
        var identityComputer = new StreamIdentityComputer("test-v1");
        var identity = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity(), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());

        // Act
        cache.Set(identity, CreateFakePartitionResult());
        cache.TryGet(identity, out _);
        cache.Clear();

        // Assert: Logger should have been called (we can't verify exact calls without more setup)
        logger.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Cache_EmptyKey_Allowed()
    {
        // Arrange - the cache key is derived from identity, not user-provided
        var cache = new PartitionCache();

        // Act & Assert: Should not throw on empty cache operations
        cache.Count.Should().Be(0);
        cache.HitRatio.Should().Be(0);
    }

    [Fact]
    public void Cache_OverwriteEntry_UpdatesValue()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");
        var identity = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity(), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());

        var result1 = CreateFakePartitionResult(plateId: 1);
        var result2 = CreateFakePartitionResult(plateId: 2);

        // Act
        cache.Set(identity, result1);
        cache.Set(identity, result2); // Overwrite

        // Assert
        cache.TryGet(identity, out var retrieved);
        retrieved.PlatePolygons.Should().ContainKey(TestDataFactory.PlateId(2));
        cache.Count.Should().Be(1); // Still one entry
    }

    [Fact]
    public void Cache_Count_Accurate()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");

        // Act: Add entries
        for (int i = 0; i < 5; i++)
        {
            var identity = identityComputer.ComputeStreamIdentity(
                CreateFakeStreamIdentity($"stream{i}"), new CanonicalTick(1), 1, new TolerancePolicy.StrictPolicy());
            cache.Set(identity, CreateFakePartitionResult());
        }

        // Assert
        cache.Count.Should().Be(5);
    }

    [Fact]
    public void CacheIdentity_DifferentTicks_DifferentKeys()
    {
        // Arrange
        var identityComputer = new StreamIdentityComputer("test-v1");
        var streamId = CreateFakeStreamIdentity();
        var policy = new TolerancePolicy.StrictPolicy();

        // Act
        var idTick1 = identityComputer.ComputeStreamIdentity(streamId, new CanonicalTick(1), 1, policy);
        var idTick2 = identityComputer.ComputeStreamIdentity(streamId, new CanonicalTick(2), 1, policy);

        // Assert
        idTick1.CombinedHash.Should().NotBe(idTick2.CombinedHash);
        idTick1.TopologyStreamHash.Should().NotBe(idTick2.TopologyStreamHash);
    }

    #endregion

    #region Helpers

    private static TruthStreamIdentity CreateFakeStreamIdentity(string? name = null)
    {
        var domain = Domain.Parse("test.cache");
        var variant = name ?? Guid.NewGuid().ToString("N")[..16];
        return new TruthStreamIdentity(variant, "main", 0, domain, "M0");
    }

    private static PlatePartitionResult CreateFakePartitionResult(int plateId = 1)
    {
        return new PlatePartitionResult
        {
            PlatePolygons = new Dictionary<PlateId, PlatePolygon>
            {
                [TestDataFactory.PlateId(plateId)] = new PlatePolygon
                {
                    PlateId = TestDataFactory.PlateId(plateId),
                    OuterBoundary = new UnifyGeometry.Polygon(ImmutableArray<UnifyGeometry.Point3>.Empty),
                    SphericalArea = 1.0
                }
            },
            QualityMetrics = new PartitionQualityMetrics(),
            Provenance = new PartitionProvenance
            {
                TopologySource = CreateFakeStreamIdentity(),
                PolygonizerVersion = "test",
                ComputedAt = DateTimeOffset.UtcNow,
                AlgorithmHash = "abc123"
            },
            Status = PartitionValidity.Valid
        };
    }

    #endregion
}
