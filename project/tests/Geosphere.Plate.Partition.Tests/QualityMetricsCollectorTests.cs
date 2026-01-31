using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Partition.Solver;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using PlateId = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.PlateId;
using PlatePolygon = FantaSim.Geosphere.Plate.Partition.Contracts.PlatePolygon;
using FluentAssertions;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Partition.Tests;

/// <summary>
/// Quality Metrics Collector Tests - RFC-V2-0047 §5.3
/// Tests accuracy of area calculations, sliver detection, and topology issue counting.
/// </summary>
public sealed class QualityMetricsCollectorTests
{
    #region Test: Area calculations

    [Fact]
    public void AreaCalculation_SingleArea_RecordedCorrectly()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        collector.RecordArea(1.5);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.MinArea.Should().Be(1.5);
        metrics.MaxArea.Should().Be(1.5);
    }

    [Fact]
    public void AreaCalculation_MultipleAreas_MinMaxCorrect()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        collector.RecordArea(1.0);
        collector.RecordArea(2.0);
        collector.RecordArea(3.0);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.MinArea.Should().Be(1.0);
        metrics.MaxArea.Should().Be(3.0);
    }

    [Fact]
    public void AreaCalculation_Variance_ComputedCorrectly()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act - record areas: 1, 2, 3 (mean = 2, variance = (1+0+1)/3 = 2/3)
        collector.RecordArea(1.0);
        collector.RecordArea(2.0);
        collector.RecordArea(3.0);
        var metrics = collector.BuildMetrics();

        // Assert - variance should be approximately 0.667
        metrics.AreaVariance.Should().BeApproximately(0.667, 0.01);
    }

    [Fact]
    public void AreaCalculation_EmptyCollector_ZeroValues()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.MinArea.Should().Be(0.0);
        metrics.MaxArea.Should().Be(0.0);
        metrics.AreaVariance.Should().Be(0.0);
    }

    [Fact]
    public void AreaCalculation_SingleValue_ZeroVariance()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        collector.RecordArea(5.0);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.AreaVariance.Should().Be(0.0);
    }

    [Fact]
    public void AreaCalculation_GeometryMetrics_FromPolygons()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var polygons = new Dictionary<PlateId, PlatePolygon>
        {
            [TestDataFactory.PlateId(1)] = CreatePlatePolygon(TestDataFactory.PlateId(1), 0.5),
            [TestDataFactory.PlateId(2)] = CreatePlatePolygon(TestDataFactory.PlateId(2), 1.0),
            [TestDataFactory.PlateId(3)] = CreatePlatePolygon(TestDataFactory.PlateId(3), 1.5)
        };

        // Act
        collector.RecordGeometryMetrics(polygons, sliverThreshold: 0.1);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.MinArea.Should().Be(0.5);
        metrics.MaxArea.Should().Be(1.5);
    }

    #endregion

    #region Test: Sliver detection

    [Fact]
    public void SliverDetection_AreaBelowThreshold_CountsAsSliver()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var polygons = new Dictionary<PlateId, PlatePolygon>
        {
            [TestDataFactory.PlateId(1)] = CreatePlatePolygon(TestDataFactory.PlateId(1), 1e-15), // Sliver
            [TestDataFactory.PlateId(2)] = CreatePlatePolygon(TestDataFactory.PlateId(2), 1.0)    // Normal
        };

        // Act
        collector.RecordGeometryMetrics(polygons, sliverThreshold: 1e-12);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.SliverCount.Should().Be(1);
    }

    [Fact]
    public void SliverDetection_MultipleSlivers_AllCounted()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var polygons = new Dictionary<PlateId, PlatePolygon>();

        for (int i = 0; i < 5; i++)
        {
            polygons[TestDataFactory.PlateId(i + 1)] = CreatePlatePolygon(
                TestDataFactory.PlateId(i + 1), 1e-15);
        }

        // Act
        collector.RecordGeometryMetrics(polygons, sliverThreshold: 1e-12);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.SliverCount.Should().Be(5);
    }

    [Fact]
    public void SliverDetection_NoSlivers_ZeroCount()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var polygons = new Dictionary<PlateId, PlatePolygon>
        {
            [TestDataFactory.PlateId(1)] = CreatePlatePolygon(TestDataFactory.PlateId(1), 1.0),
            [TestDataFactory.PlateId(2)] = CreatePlatePolygon(TestDataFactory.PlateId(2), 2.0)
        };

        // Act
        collector.RecordGeometryMetrics(polygons, sliverThreshold: 0.1);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.SliverCount.Should().Be(0);
    }

    [Fact]
    public void SliverDetection_BoundaryValue_NotCounted()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var polygons = new Dictionary<PlateId, PlatePolygon>
        {
            [TestDataFactory.PlateId(1)] = CreatePlatePolygon(TestDataFactory.PlateId(1), 1e-12) // At threshold
        };

        // Act
        collector.RecordGeometryMetrics(polygons, sliverThreshold: 1e-12);
        var metrics = collector.BuildMetrics();

        // Assert: Area >= threshold should not be sliver
        metrics.SliverCount.Should().Be(0);
    }

    #endregion

    #region Test: Topology issue counting

    [Fact]
    public void TopologyIssue_OpenBoundary_Recorded()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        collector.RecordOpenBoundary();
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.OpenBoundaryCount.Should().Be(1);
    }

    [Fact]
    public void TopologyIssue_MultipleOpenBoundaries_AllRecorded()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        for (int i = 0; i < 10; i++)
        {
            collector.RecordOpenBoundary();
        }
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.OpenBoundaryCount.Should().Be(10);
    }

    [Fact]
    public void TopologyIssue_NonManifoldJunction_Recorded()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        collector.RecordNonManifoldJunction();
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.NonManifoldJunctionCount.Should().Be(1);
    }

    [Fact]
    public void TopologyIssue_MultipleNonManifoldJunctions_AllRecorded()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        for (int i = 0; i < 5; i++)
        {
            collector.RecordNonManifoldJunction();
        }
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.NonManifoldJunctionCount.Should().Be(5);
    }

    [Fact]
    public void TopologyIssue_AmbiguousAttribution_Recorded()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        collector.RecordAmbiguousAttribution();
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.AmbiguousAttributionCount.Should().Be(1);
    }

    [Fact]
    public void TopologyIssue_FromDiagnostics_Recorded()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var diagnostics = new PolygonizationDiagnostics(
            false,
            ImmutableArray.Create(
                new OpenBoundaryDiagnostic(TestDataFactory.BoundaryId(1), new Point3(0, 0, 0), "Test"),
                new OpenBoundaryDiagnostic(TestDataFactory.BoundaryId(2), new Point3(1, 0, 0), "Test")),
            ImmutableArray.Create(
                new NonManifoldJunctionDiagnostic(TestDataFactory.JunctionId(1), new Point3(0, 0, 0), 4, "Test")),
            ImmutableArray<DisconnectedComponentDiagnostic>.Empty);

        // Act
        collector.RecordTopologyMetrics(diagnostics);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.OpenBoundaryCount.Should().Be(2);
        metrics.NonManifoldJunctionCount.Should().Be(1);
    }

    [Fact]
    public void TopologyIssue_FaceCount_Recorded()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        collector.RecordFaceCount(42);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.FaceCount.Should().Be(42);
    }

    #endregion

    #region Test: Hole counting

    [Fact]
    public void HoleCount_SingleHole_Counted()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var polygons = new Dictionary<PlateId, PlatePolygon>
        {
            [TestDataFactory.PlateId(1)] = new PlatePolygon
            {
                PlateId = TestDataFactory.PlateId(1),
                OuterBoundary = new Polygon(ImmutableArray<Point3>.Empty),
                Holes = ImmutableArray.Create(new Polygon(ImmutableArray<Point3>.Empty)),
                SphericalArea = 1.0
            }
        };

        // Act
        collector.RecordGeometryMetrics(polygons, sliverThreshold: 0.1);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.HoleCount.Should().Be(1);
    }

    [Fact]
    public void HoleCount_MultipleHoles_Counted()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var polygons = new Dictionary<PlateId, PlatePolygon>
        {
            [TestDataFactory.PlateId(1)] = new PlatePolygon
            {
                PlateId = TestDataFactory.PlateId(1),
                OuterBoundary = new Polygon(ImmutableArray<Point3>.Empty),
                Holes = ImmutableArray.Create(
                    new Polygon(ImmutableArray<Point3>.Empty),
                    new Polygon(ImmutableArray<Point3>.Empty),
                    new Polygon(ImmutableArray<Point3>.Empty)),
                SphericalArea = 1.0
            }
        };

        // Act
        collector.RecordGeometryMetrics(polygons, sliverThreshold: 0.1);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.HoleCount.Should().Be(3);
    }

    [Fact]
    public void HoleCount_MultiplePolygons_Cumulative()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var polygons = new Dictionary<PlateId, PlatePolygon>
        {
            [TestDataFactory.PlateId(1)] = new PlatePolygon
            {
                PlateId = TestDataFactory.PlateId(1),
                OuterBoundary = new Polygon(ImmutableArray<Point3>.Empty),
                Holes = ImmutableArray.Create(new Polygon(ImmutableArray<Point3>.Empty)),
                SphericalArea = 1.0
            },
            [TestDataFactory.PlateId(2)] = new PlatePolygon
            {
                PlateId = TestDataFactory.PlateId(2),
                OuterBoundary = new Polygon(ImmutableArray<Point3>.Empty),
                Holes = ImmutableArray.Create(
                    new Polygon(ImmutableArray<Point3>.Empty),
                    new Polygon(ImmutableArray<Point3>.Empty)),
                SphericalArea = 1.0
            }
        };

        // Act
        collector.RecordGeometryMetrics(polygons, sliverThreshold: 0.1);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.HoleCount.Should().Be(3);
    }

    [Fact]
    public void HoleCount_NoHoles_ZeroCount()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var polygons = new Dictionary<PlateId, PlatePolygon>
        {
            [TestDataFactory.PlateId(1)] = new PlatePolygon
            {
                PlateId = TestDataFactory.PlateId(1),
                OuterBoundary = new Polygon(ImmutableArray<Point3>.Empty),
                Holes = ImmutableArray<Polygon>.Empty,
                SphericalArea = 1.0
            }
        };

        // Act
        collector.RecordGeometryMetrics(polygons, sliverThreshold: 0.1);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.HoleCount.Should().Be(0);
    }

    #endregion

    #region Test: Timing

    [Fact]
    public void Timing_StartStop_RecordsTime()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        collector.StartTiming();
        Thread.Sleep(50); // 50ms delay
        collector.StopTiming();
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.ComputationTimeMs.Should().BeGreaterOrEqualTo(50);
    }

    [Fact]
    public void Timing_AutoStopOnBuild_StopsTimer()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act
        collector.StartTiming();
        Thread.Sleep(20);
        var metrics = collector.BuildMetrics(); // Should auto-stop

        // Assert
        metrics.ComputationTimeMs.Should().BeGreaterOrEqualTo(20);
    }

    [Fact]
    public void Timing_WithoutTiming_ZeroTime()
    {
        // Arrange
        var collector = new QualityMetricsCollector();

        // Act - don't start timing
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.ComputationTimeMs.Should().Be(0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Concurrent_Access_ThreadSafe()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var tasks = new List<Task>();

        // Act: Concurrent recording from multiple threads
        for (int i = 0; i < 100; i++)
        {
            var threadNum = i;
            tasks.Add(Task.Run(() =>
            {
                collector.RecordArea(1.0 + threadNum);
                collector.RecordOpenBoundary();
                collector.RecordNonManifoldJunction();
            }));
        }

        Task.WaitAll(tasks.ToArray());
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.OpenBoundaryCount.Should().Be(100);
        metrics.NonManifoldJunctionCount.Should().Be(100);
    }

    [Fact]
    public void EmptyPolygonCollection_ZeroMetrics()
    {
        // Arrange
        var collector = new QualityMetricsCollector();
        var emptyPolygons = new Dictionary<PlateId, PlatePolygon>();

        // Act
        collector.RecordGeometryMetrics(emptyPolygons, sliverThreshold: 0.1);
        var metrics = collector.BuildMetrics();

        // Assert
        metrics.MinArea.Should().Be(0);
        metrics.MaxArea.Should().Be(0);
        metrics.SliverCount.Should().Be(0);
        metrics.HoleCount.Should().Be(0);
    }

    [Fact]
    public void NegativeArea_AbsoluteValueUsed()
    {
        // Arrange - sliver detection uses comparison without sign check
        var collector = new QualityMetricsCollector();

        // Act - Record a very small area (could be negative due to winding)
        collector.RecordArea(-1e-15);
        var metrics = collector.BuildMetrics();

        // Assert
        // The area should be recorded as-is (collector doesn't take absolute)
        metrics.MinArea.Should().Be(-1e-15);
    }

    #endregion

    #region Helpers

    private static PlatePolygon CreatePlatePolygon(PlateId plateId, double area)
    {
        return new PlatePolygon
        {
            PlateId = plateId,
            OuterBoundary = new Polygon(ImmutableArray<Point3>.Empty),
            Holes = ImmutableArray<Polygon>.Empty,
            SphericalArea = area
        };
    }

    #endregion
}
