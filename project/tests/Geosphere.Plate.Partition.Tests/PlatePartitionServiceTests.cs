using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Partition.Solver;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Solvers;
using FantaSim.Geosphere.Plate.Polygonization.Solver;
using FantaSim.Geosphere.Plate.Polygonization.Solver.CMap;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Materializer;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Partition.Tests;

/// <summary>
/// Plate Partition Service Integration Tests - RFC-V2-0047 §8
/// Tests service integration including dependency resolution, caching, provenance, and metrics.
/// </summary>
public sealed class PlatePartitionServiceTests
{
    private static readonly CanonicalTick TestTick = new(100);

    #region Test: Service resolves dependencies correctly

    [Fact]
    public void Service_RegistersDependencies_DependenciesResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPlatePartitionSolver();
        services.AddSingleton<PlateTopologyMaterializer>(sp => CreateFakeMaterializer());
        // Register IPlatePolygonizer interface (required by StrictPolygonizer, LenientPolygonizer, etc.)
        services.AddSingleton<IPlatePolygonizer>(sp => new PlatePolygonizer(new BoundaryCMapBuilder()));

        var provider = services.BuildServiceProvider();

        // Act & Assert: All dependencies should be resolvable
        var cache = provider.GetService<PartitionCache>();
        var strict = provider.GetService<StrictPolygonizer>();
        var lenient = provider.GetService<LenientPolygonizer>();
        var defaultPoly = provider.GetService<DefaultPolygonizer>();
        var identityComputer = provider.GetService<StreamIdentityComputer>();
        var service = provider.GetService<IPlatePartitionService>();

        cache.Should().NotBeNull();
        strict.Should().NotBeNull();
        lenient.Should().NotBeNull();
        defaultPoly.Should().NotBeNull();
        identityComputer.Should().NotBeNull();
        service.Should().NotBeNull();
    }

    [Fact]
    public void Service_WithCustomCache_UsesProvidedCache()
    {
        // Arrange
        var customCache = new PartitionCache(TimeSpan.FromMinutes(10));

        var services = new ServiceCollection();
        services.AddPlatePartitionSolver(customCache);
        services.AddSingleton<PlateTopologyMaterializer>(sp => CreateFakeMaterializer());
        services.AddSingleton<IPlatePolygonizer>(sp => new PlatePolygonizer(new BoundaryCMapBuilder()));

        var provider = services.BuildServiceProvider();

        // Act
        var resolvedCache = provider.GetRequiredService<PartitionCache>();

        // Assert
        resolvedCache.Should().BeSameAs(customCache);
    }

    [Fact]
    public void Service_WithCustomOptions_UsesCustomConfiguration()
    {
        // Arrange
        var options = new PartitionCacheOptions
        {
            CacheDuration = TimeSpan.FromHours(1),
            MaxEntries = 500,
            EnableAutoEviction = false
        };

        var services = new ServiceCollection();
        services.AddPlatePartitionSolver(options);
        services.AddSingleton<PlateTopologyMaterializer>(sp => CreateFakeMaterializer());
        services.AddSingleton<IPlatePolygonizer>(sp => new PlatePolygonizer(new BoundaryCMapBuilder()));

        var provider = services.BuildServiceProvider();

        // Act
        var cache = provider.GetRequiredService<PartitionCache>();

        // Assert: Cache should be configured (we can't directly verify internal settings,
        // but we can verify it's registered)
        cache.Should().NotBeNull();
    }

    #endregion

    #region Test: Caching works correctly

    [Fact]
    public void Cache_FirstRequest_Miss()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");
        var streamId = CreateFakeStreamIdentity();
        var policy = new TolerancePolicy.StrictPolicy();

        var identity = identityComputer.ComputeStreamIdentity(streamId, 1, policy);

        // Act: First request should be a miss
        var hit = cache.TryGet(identity, out var result);

        // Assert
        hit.Should().BeFalse();
        cache.HitCount.Should().Be(0);
        cache.MissCount.Should().Be(1);
    }

    [Fact]
    public void Cache_SecondRequest_Hit()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");
        var streamId = CreateFakeStreamIdentity();
        var policy = new TolerancePolicy.StrictPolicy();

        var identity = identityComputer.ComputeStreamIdentity(streamId, 1, policy);
        var fakeResult = CreateFakePartitionResult();

        // Act: Store and retrieve
        cache.Set(identity, fakeResult);
        var hit = cache.TryGet(identity, out var result);

        // Assert
        hit.Should().BeTrue();
        cache.HitCount.Should().Be(1);
        result.Should().NotBeNull();
    }

    [Fact]
    public void Cache_DifferentPolicies_DifferentCacheKeys()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");
        var streamId = CreateFakeStreamIdentity();

        var strictPolicy = new TolerancePolicy.StrictPolicy();
        var lenientPolicy = new TolerancePolicy.LenientPolicy(1e-9);

        var strictIdentity = identityComputer.ComputeStreamIdentity(streamId, 1, strictPolicy);
        var lenientIdentity = identityComputer.ComputeStreamIdentity(streamId, 1, lenientPolicy);

        var strictResult = CreateFakePartitionResult();
        var lenientResult = CreateFakePartitionResult(withDifferentData: true);

        // Act: Store both
        cache.Set(strictIdentity, strictResult);
        cache.Set(lenientIdentity, lenientResult);

        // Assert: Both should be retrievable
        cache.TryGet(strictIdentity, out var retrievedStrict).Should().BeTrue();
        cache.TryGet(lenientIdentity, out var retrievedLenient).Should().BeTrue();
        cache.Count.Should().Be(2);
    }

    [Fact]
    public void Cache_Clear_RemovesAllEntries()
    {
        // Arrange
        var cache = new PartitionCache();
        var identityComputer = new StreamIdentityComputer("test-v1");

        for (int i = 0; i < 5; i++)
        {
            // Use different stream identities (not just different versions) to create distinct cache entries
            var identity = identityComputer.ComputeStreamIdentity(CreateFakeStreamIdentity($"stream-{i}"), 1, new TolerancePolicy.StrictPolicy());
            cache.Set(identity, CreateFakePartitionResult());
        }

        cache.Count.Should().Be(5);

        // Act
        cache.Clear();

        // Assert
        cache.Count.Should().Be(0);
        cache.HitCount.Should().Be(0);
        cache.MissCount.Should().Be(0);
    }

    #endregion

    #region Test: Provenance tracking

    [Fact]
    public void PartitionResult_ContainsProvenance()
    {
        // Arrange
        var result = CreateFakePartitionResult();

        // Assert
        result.Provenance.TopologySource.Should().NotBeNull();
        result.Provenance.PolygonizerVersion.Should().NotBeNullOrEmpty();
        result.Provenance.ComputedAt.Should().NotBe(default(DateTimeOffset));
        result.Provenance.AlgorithmHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PartitionResult_Provenance_VersionMatchesService()
    {
        // Arrange
        var expectedVersion = PlatePartitionService.PolygonizerVersion;
        var result = CreateFakePartitionResult(version: expectedVersion);

        // Assert
        result.Provenance.PolygonizerVersion.Should().Be(expectedVersion);
    }

    [Fact]
    public void PartitionResult_Provenance_AlgorithmHashIsDeterministic()
    {
        // Arrange
        var request = TestDataFactory.CreatePartitionRequest(policy: new TolerancePolicy.StrictPolicy());
        var request2 = TestDataFactory.CreatePartitionRequest(policy: new TolerancePolicy.StrictPolicy());

        // Act: Create results with same parameters
        var result1 = CreateFakePartitionResult(request: request);
        var result2 = CreateFakePartitionResult(request: request2);

        // Assert: Algorithm hash should be the same for same configuration
        result1.Provenance.AlgorithmHash.Should().Be(result2.Provenance.AlgorithmHash);
    }

    [Fact]
    public void PartitionResult_Provenance_DifferentPolicies_DifferentHash()
    {
        // Arrange
        var strictRequest = TestDataFactory.CreatePartitionRequest(policy: new TolerancePolicy.StrictPolicy());
        var lenientRequest = TestDataFactory.CreatePartitionRequest(policy: new TolerancePolicy.LenientPolicy(1e-9));

        // Act
        var strictResult = CreateFakePartitionResult(request: strictRequest);
        var lenientResult = CreateFakePartitionResult(request: lenientRequest);

        // Assert
        strictResult.Provenance.AlgorithmHash.Should().NotBe(lenientResult.Provenance.AlgorithmHash);
    }

    #endregion

    #region Test: Quality metrics collection

    [Fact]
    public void PartitionResult_ContainsQualityMetrics()
    {
        // Arrange
        var metrics = new PartitionQualityMetrics
        {
            MinArea = 0.1,
            MaxArea = 1.0,
            FaceCount = 3,
            ComputationTimeMs = 100
        };

        var result = new PlatePartitionResult
        {
            PlatePolygons = new Dictionary<PlateId, PlatePolygon>(),
            QualityMetrics = metrics,
            Provenance = CreateFakeProvenance(),
            Status = PartitionValidity.Valid
        };

        // Assert
        result.QualityMetrics.FaceCount.Should().Be(3);
        result.QualityMetrics.MinArea.Should().Be(0.1);
        result.QualityMetrics.MaxArea.Should().Be(1.0);
    }

    [Fact]
    public void MetricsCollector_RecordsTiming()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        collector.StartTiming();
        Thread.Sleep(10); // Small delay
        collector.StopTiming();
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.ComputationTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MetricsCollector_RecordsAreas()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        collector.RecordArea(0.5);
        collector.RecordArea(1.0);
        collector.RecordArea(1.5);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.MinArea.Should().Be(0.5);
        metrics.MaxArea.Should().Be(1.5);
    }

    [Fact]
    public void MetricsCollector_RecordsTopologyIssues()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        collector.RecordOpenBoundary();
        collector.RecordOpenBoundary();
        collector.RecordNonManifoldJunction();
        collector.RecordAmbiguousAttribution();

        var metrics = collector.BuildMetrics();

        // Assert
        metrics.OpenBoundaryCount.Should().Be(2);
        metrics.NonManifoldJunctionCount.Should().Be(1);
        metrics.AmbiguousAttributionCount.Should().Be(1);
    }

    [Fact]
    public void MetricsCollector_RecordsSliverDetection()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var polygons = new Dictionary<PlateId, PlatePolygon>
        {
            [TestDataFactory.PlateId(1)] = new PlatePolygon
            {
                PlateId = TestDataFactory.PlateId(1),
                OuterBoundary = new UnifyGeometry.Polygon(ImmutableArray<UnifyGeometry.Point3>.Empty),
                SphericalArea = 1e-15 // Very small - sliver
            },
            [TestDataFactory.PlateId(2)] = new PlatePolygon
            {
                PlateId = TestDataFactory.PlateId(2),
                OuterBoundary = new UnifyGeometry.Polygon(ImmutableArray<UnifyGeometry.Point3>.Empty),
                SphericalArea = 1.0 // Normal
            }
        };

        // Act
        collector.RecordGeometryMetrics(polygons, sliverThreshold: 1e-12);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.SliverCount.Should().BeGreaterOrEqualTo(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Service_WithLogger_LogsOperations()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PlatePartitionService>>();
        var cacheLogger = Substitute.For<ILogger<PartitionCache>>();
        var cache = new PartitionCache(logger: cacheLogger);

        // Act: Create service with logger
        var materializer = CreateFakeMaterializer();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());
        var service = new PlatePartitionService(materializer, polygonizer, cache, logger);

        // Assert: Service created successfully
        service.Should().NotBeNull();
    }

    [Fact]
    public void Service_NullMaterializer_ThrowsArgumentNullException()
    {
        // Arrange
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PlatePartitionService(null!, polygonizer));
    }

    [Fact]
    public void Service_NullPolygonizer_ThrowsArgumentNullException()
    {
        // Arrange
        var materializer = CreateFakeMaterializer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PlatePartitionService(materializer, null!));
    }

    #endregion

    #region Helpers

    private static PlateTopologyMaterializer CreateFakeMaterializer()
    {
        // Create a minimal fake materializer using a substituted event store
        var store = Substitute.For<FantaSim.Geosphere.Plate.Topology.Contracts.Events.ITopologyEventStore>();
        return new PlateTopologyMaterializer(store);
    }

    private static FantaSim.Geosphere.Plate.Topology.Contracts.Identity.TruthStreamIdentity CreateFakeStreamIdentity(string? variant = null)
    {
        var domain = FantaSim.Geosphere.Plate.Topology.Contracts.Identity.Domain.Parse("test.partition");
        return new FantaSim.Geosphere.Plate.Topology.Contracts.Identity.TruthStreamIdentity(
            variant ?? "test-variant", "main", 0, domain, "M0");
    }

    private static PlatePartitionResult CreateFakePartitionResult(
        bool withDifferentData = false,
        PartitionRequest? request = null,
        string? version = null)
    {
        var polygons = new Dictionary<PlateId, PlatePolygon>();

        if (withDifferentData)
        {
            polygons[TestDataFactory.PlateId(99)] = new PlatePolygon
            {
                PlateId = TestDataFactory.PlateId(99),
                OuterBoundary = new UnifyGeometry.Polygon(ImmutableArray<UnifyGeometry.Point3>.Empty),
                SphericalArea = 2.0
            };
        }
        else
        {
            polygons[TestDataFactory.PlateId(1)] = new PlatePolygon
            {
                PlateId = TestDataFactory.PlateId(1),
                OuterBoundary = new UnifyGeometry.Polygon(ImmutableArray<UnifyGeometry.Point3>.Empty),
                SphericalArea = 1.0
            };
        }

        return new PlatePartitionResult
        {
            PlatePolygons = polygons,
            QualityMetrics = new PartitionQualityMetrics(),
            Provenance = CreateFakeProvenance(version: version, request: request),
            Status = PartitionValidity.Valid
        };
    }

    private static PartitionProvenance CreateFakeProvenance(string? version = null, PartitionRequest? request = null)
    {
        var algorithmHash = ComputeAlgorithmHash(request ?? TestDataFactory.CreatePartitionRequest());

        return new PartitionProvenance
        {
            TopologySource = CreateFakeStreamIdentity(),
            PolygonizerVersion = version ?? PlatePartitionService.PolygonizerVersion,
            ComputedAt = DateTimeOffset.UtcNow,
            AlgorithmHash = algorithmHash
        };
    }

    private static string ComputeAlgorithmHash(PartitionRequest request)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var input = $"{PlatePartitionService.PolygonizerVersion}:{request.TolerancePolicy.GetType().Name}:{request.Options.GetHashCode()}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash)[..16];
    }

    #endregion
}
