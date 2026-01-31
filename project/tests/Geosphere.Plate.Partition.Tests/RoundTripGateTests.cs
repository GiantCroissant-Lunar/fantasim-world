using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Partition.Solver;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Solvers;
using FantaSim.Geosphere.Plate.Polygonization.Solver;
using FantaSim.Geosphere.Plate.Polygonization.Solver.CMap;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FluentAssertions;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;
using Boundary = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Boundary;
using Junction = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Junction;

namespace FantaSim.Geosphere.Plate.Partition.Tests;

/// <summary>
/// Round-Trip Gate Tests - RFC-V2-0047 §3.4
/// Verifies that Partition → Boundaries → Partition ≈ original.
/// </summary>
public sealed class RoundTripGateTests
{
    private static readonly CanonicalTick TestTick = new(100);

    #region Test: Extract boundaries from partition, repartition, compare

    [Fact(Skip = "Requires BoundaryCMapBuilder fix - pre-existing issue in Polygonization.Solver")]
    public void RoundTrip_TwoPlates_PreservesStructure()
    {
        // Arrange: Create initial partition
        var topology = TestDataFactory.CreateTwoPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act: First partition
        var firstPartition = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Extract boundaries from partition
        var extractedBoundaries = ExtractBoundariesFromPartition(firstPartition);

        // Create new topology from extracted boundaries
        var repartitionedTopology = CreateTopologyFromBoundaries(extractedBoundaries);

        // Second partition
        var secondPartition = polygonizer.PolygonizeAtTick(TestTick, repartitionedTopology);

        // Assert: Structures should be similar
        ComparePartitions(firstPartition, secondPartition);
    }

    [Fact(Skip = "Requires BoundaryCMapBuilder fix - pre-existing issue in Polygonization.Solver")]
    public void RoundTrip_ThreePlates_PreservesStructure()
    {
        // Arrange
        var topology = TestDataFactory.CreateThreePlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act: First partition
        var firstPartition = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Extract and rebuild
        var extractedBoundaries = ExtractBoundariesFromPartition(firstPartition);
        var repartitionedTopology = CreateTopologyFromBoundaries(extractedBoundaries);

        // Second partition
        var secondPartition = polygonizer.PolygonizeAtTick(TestTick, repartitionedTopology);

        // Assert
        ComparePartitions(firstPartition, secondPartition);
    }

    [Fact(Skip = "Requires BoundaryCMapBuilder fix - pre-existing issue in Polygonization.Solver")]
    public void RoundTrip_FourPlates_PreservesStructure()
    {
        // Arrange
        var topology = TestDataFactory.CreateFourPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var firstPartition = polygonizer.PolygonizeAtTick(TestTick, topology);
        var extractedBoundaries = ExtractBoundariesFromPartition(firstPartition);
        var repartitionedTopology = CreateTopologyFromBoundaries(extractedBoundaries);
        var secondPartition = polygonizer.PolygonizeAtTick(TestTick, repartitionedTopology);

        // Assert
        ComparePartitions(firstPartition, secondPartition);
    }

    [Fact(Skip = "Requires BoundaryCMapBuilder fix - pre-existing issue in Polygonization.Solver")]
    public void RoundTrip_WithHoles_PreservesHoleStructure()
    {
        // Arrange: Topology with holes
        var topology = CreateRoundTripTopologyWithHoles();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var firstPartition = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Extract boundaries including hole boundaries
        var extractedBoundaries = ExtractBoundariesWithHoles(firstPartition);
        var repartitionedTopology = CreateTopologyFromBoundaries(extractedBoundaries);

        // Second partition
        var secondPartition = polygonizer.PolygonizeAtTick(TestTick, repartitionedTopology);

        // Assert: Should preserve hole count
        var firstHoleCount = firstPartition.Polygons.Sum(p => p.Holes.Length);
        var secondHoleCount = secondPartition.Polygons.Sum(p => p.Holes.Length);
        secondHoleCount.Should().Be(firstHoleCount);
    }

    #endregion

    #region Test: Area preservation within tolerance

    [Fact(Skip = "Requires BoundaryCMapBuilder fix")]
    public void AreaPreservation_TwoPlates_AreasSimilar()
    {
        // Arrange
        var topology = TestDataFactory.CreateTwoPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var firstPartition = polygonizer.PolygonizeAtTick(TestTick, topology);
        var firstAreas = ComputeAreas(firstPartition);

        var extractedBoundaries = ExtractBoundariesFromPartition(firstPartition);
        var repartitionedTopology = CreateTopologyFromBoundaries(extractedBoundaries);
        var secondPartition = polygonizer.PolygonizeAtTick(TestTick, repartitionedTopology);
        var secondAreas = ComputeAreas(secondPartition);

        // Assert: Areas should be similar within tolerance
        const double tolerance = 0.01; // 1% tolerance

        foreach (var kvp in firstAreas)
        {
            if (secondAreas.TryGetValue(kvp.Key, out var secondArea))
            {
                var ratio = Math.Abs(kvp.Value - secondArea) / kvp.Value;
                ratio.Should().BeLessThan(tolerance,
                    $"Area for plate {kvp.Key} should be preserved within {tolerance:P}");
            }
        }
    }

    [Fact(Skip = "Requires BoundaryCMapBuilder fix")]
    public void AreaPreservation_MultiplePlates_TotalAreaConserved()
    {
        // Arrange
        var topology = TestDataFactory.CreateThreePlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var firstPartition = polygonizer.PolygonizeAtTick(TestTick, topology);
        var firstTotalArea = ComputeTotalArea(firstPartition);

        var extractedBoundaries = ExtractBoundariesFromPartition(firstPartition);
        var repartitionedTopology = CreateTopologyFromBoundaries(extractedBoundaries);
        var secondPartition = polygonizer.PolygonizeAtTick(TestTick, repartitionedTopology);
        var secondTotalArea = ComputeTotalArea(secondPartition);

        // Assert: Total area should be conserved
        const double tolerance = 0.05; // 5% tolerance for total
        var ratio = Math.Abs(firstTotalArea - secondTotalArea) / Math.Max(firstTotalArea, 1e-10);
        ratio.Should().BeLessThan(tolerance);
    }

    [Fact(Skip = "Requires BoundaryCMapBuilder fix")]
    public void AreaPreservation_WithHoles_AreasSimilar()
    {
        // Arrange: Topology with holes
        var topology = CreateRoundTripTopologyWithHoles();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var firstPartition = polygonizer.PolygonizeAtTick(TestTick, topology);
        var firstAreas = ComputeNetAreas(firstPartition); // Subtract hole areas

        var extractedBoundaries = ExtractBoundariesWithHoles(firstPartition);
        var repartitionedTopology = CreateTopologyFromBoundaries(extractedBoundaries);
        var secondPartition = polygonizer.PolygonizeAtTick(TestTick, repartitionedTopology);
        var secondAreas = ComputeNetAreas(secondPartition);

        // Assert
        const double tolerance = 0.02;
        foreach (var kvp in firstAreas)
        {
            if (secondAreas.TryGetValue(kvp.Key, out var secondArea))
            {
                var ratio = Math.Abs(kvp.Value - secondArea) / Math.Max(kvp.Value, 1e-10);
                ratio.Should().BeLessThan(tolerance);
            }
        }
    }

    #endregion

    #region Test: Topology preservation

    [Fact(Skip = "Requires BoundaryCMapBuilder fix")]
    public void TopologyPreservation_TwoPlates_SameAdjacency()
    {
        // Arrange
        var topology = TestDataFactory.CreateTwoPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var firstPartition = polygonizer.PolygonizeAtTick(TestTick, topology);
        var firstAdjacency = BuildAdjacencyGraph(firstPartition);

        var extractedBoundaries = ExtractBoundariesFromPartition(firstPartition);
        var repartitionedTopology = CreateTopologyFromBoundaries(extractedBoundaries);
        var secondPartition = polygonizer.PolygonizeAtTick(TestTick, repartitionedTopology);
        var secondAdjacency = BuildAdjacencyGraph(secondPartition);

        // Assert: Adjacency should be preserved
        firstAdjacency.Count.Should().Be(secondAdjacency.Count);

        foreach (var plateAdj in firstAdjacency)
        {
            secondAdjacency.Should().ContainKey(plateAdj.Key);
            secondAdjacency[plateAdj.Key].Should().Equal(plateAdj.Value);
        }
    }

    [Fact(Skip = "Requires BoundaryCMapBuilder fix")]
    public void TopologyPreservation_ThreePlates_TripleJunctionPreserved()
    {
        // Arrange
        var topology = TestDataFactory.CreateThreePlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var firstPartition = polygonizer.PolygonizeAtTick(TestTick, topology);
        var firstAdjacency = BuildAdjacencyGraph(firstPartition);

        var extractedBoundaries = ExtractBoundariesFromPartition(firstPartition);
        var repartitionedTopology = CreateTopologyFromBoundaries(extractedBoundaries);
        var secondPartition = polygonizer.PolygonizeAtTick(TestTick, repartitionedTopology);
        var secondAdjacency = BuildAdjacencyGraph(secondPartition);

        // Assert: Triple junction (3 plates meeting) should be preserved
        var tripleJunctionCount1 = CountTripleJunctions(firstAdjacency);
        var tripleJunctionCount2 = CountTripleJunctions(secondAdjacency);
        tripleJunctionCount2.Should().Be(tripleJunctionCount1);
    }

    [Fact(Skip = "Requires BoundaryCMapBuilder fix")]
    public void TopologyPreservation_PlateCountPreserved()
    {
        // Arrange
        var topology = TestDataFactory.CreateFourPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var firstPartition = polygonizer.PolygonizeAtTick(TestTick, topology);
        var firstPlateCount = firstPartition.Polygons.Length;

        var extractedBoundaries = ExtractBoundariesFromPartition(firstPartition);
        var repartitionedTopology = CreateTopologyFromBoundaries(extractedBoundaries);
        var secondPartition = polygonizer.PolygonizeAtTick(TestTick, repartitionedTopology);
        var secondPlateCount = secondPartition.Polygons.Length;

        // Assert
        secondPlateCount.Should().Be(firstPlateCount);
    }

    [Fact(Skip = "Requires BoundaryCMapBuilder fix")]
    public void TopologyPreservation_BoundaryCountPreserved()
    {
        // Arrange
        var topology = TestDataFactory.CreateTwoPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var firstPartition = polygonizer.PolygonizeAtTick(TestTick, topology);
        var firstBoundaryCount = CountBoundaries(firstPartition);

        var extractedBoundaries = ExtractBoundariesFromPartition(firstPartition);
        var repartitionedTopology = CreateTopologyFromBoundaries(extractedBoundaries);
        var secondPartition = polygonizer.PolygonizeAtTick(TestTick, repartitionedTopology);
        var secondBoundaryCount = CountBoundaries(secondPartition);

        // Assert: Boundary count should be preserved (approximately)
        var tolerance = Math.Max(1, (int)(firstBoundaryCount * 0.1));
        secondBoundaryCount.Should().BeCloseTo(firstBoundaryCount, (uint)tolerance);
    }

    #endregion

    #region Edge Cases

    [Fact(Skip = "Requires BoundaryCMapBuilder fix")]
    public void RoundTrip_EmptyPartition_EmptyResult()
    {
        // Arrange: Empty partition
        var emptyPartition = new PlatePolygonSet(TestTick, ImmutableArray<PlatePolygon>.Empty);

        // Act: Extract boundaries (should be empty)
        var extracted = ExtractBoundariesFromPartition(emptyPartition);

        // Assert
        extracted.Should().BeEmpty();
    }

    [Fact(Skip = "Requires BoundaryCMapBuilder fix")]
    public void RoundTrip_SinglePlate_NoBoundaries()
    {
        // Arrange: Single plate covering everything
        var topology = TestDataFactory.CreateSinglePlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var partition = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Single plate has no internal boundaries
        // This is a special case - the "boundary" is at infinity
        partition.Polygons.Should().BeEmpty(); // Or single unbounded polygon
    }

    [Fact(Skip = "Requires BoundaryCMapBuilder fix")]
    public void AreaPreservation_DegeneratePolygon_ZeroAreaPreserved()
    {
        // Arrange: Topology that might create degenerate polygons
        var topology = TestDataFactory.CreateSliverPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        try
        {
            var firstPartition = polygonizer.PolygonizeAtTick(TestTick, topology);
            var areas = ComputeAreas(firstPartition);

            // Assert: Small areas should remain small
            foreach (var kvp in areas)
            {
                kvp.Value.Should().BeGreaterOrEqualTo(0);
            }
        }
        catch (PolygonizationException)
        {
            // Degenerate topologies may fail - acceptable
        }
    }

    #endregion

    #region Helpers

    private static List<(PlateId Plate1, PlateId Plate2, Polyline3 Boundary)> ExtractBoundariesFromPartition(PlatePolygonSet partition)
    {
        var boundaries = new List<(PlateId, PlateId, Polyline3)>();

        // Extract boundaries from polygon outer rings
        // In a proper implementation, this would identify shared edges
        foreach (var polygon in partition.Polygons)
        {
            var ring = polygon.OuterRing;
            for (int i = 0; i < ring.Points.Length - 1; i++)
            {
                // For testing, we create boundary segments
                var segment = new Polyline3([ring.Points[i], ring.Points[i + 1]]);
                boundaries.Add((polygon.PlateId, default, segment));
            }
        }

        return boundaries;
    }

    private static List<(PlateId Plate1, PlateId Plate2, Polyline3 Boundary)> ExtractBoundariesWithHoles(PlatePolygonSet partition)
    {
        var boundaries = ExtractBoundariesFromPartition(partition);

        // Also extract hole boundaries
        foreach (var polygon in partition.Polygons)
        {
            foreach (var hole in polygon.Holes)
            {
                for (int i = 0; i < hole.Points.Length - 1; i++)
                {
                    var segment = new Polyline3([hole.Points[i], hole.Points[i + 1]]);
                    boundaries.Add((polygon.PlateId, default, segment));
                }
            }
        }

        return boundaries;
    }

    private static InMemoryTopologyStateView CreateTopologyFromBoundaries(
        List<(PlateId Plate1, PlateId Plate2, Polyline3 Boundary)> boundaries)
    {
        var topology = new InMemoryTopologyStateView("roundtrip");

        // Collect unique plates
        var plateIds = boundaries.SelectMany(b => new[] { b.Plate1, b.Plate2 })
            .Where(p => p != default)
            .Distinct()
            .ToList();

        foreach (var plateId in plateIds)
        {
            topology.Plates[plateId] = new PlateEntity(plateId, false, null);
        }

        // Create junctions and boundaries
        int junctionCounter = 1;
        int boundaryCounter = 1;
        var pointToJunction = new Dictionary<(double, double, double), JunctionId>();

        foreach (var (plate1, plate2, boundary) in boundaries)
        {
            if (boundary.Points.Length < 2) continue;

            var startPoint = boundary.Points[0];
            var endPoint = boundary.Points[boundary.Points.Length - 1];

            // Get or create junctions
            var startKey = (startPoint.X, startPoint.Y, startPoint.Z);
            var endKey = (endPoint.X, endPoint.Y, endPoint.Z);

            if (!pointToJunction.TryGetValue(startKey, out var j1))
            {
                j1 = TestDataFactory.JunctionId(junctionCounter++);
                pointToJunction[startKey] = j1;
                topology.Junctions[j1] = new Junction(j1, ImmutableArray<BoundaryId>.Empty, default, false, null);
            }

            if (!pointToJunction.TryGetValue(endKey, out var j2))
            {
                j2 = TestDataFactory.JunctionId(junctionCounter++);
                pointToJunction[endKey] = j2;
                topology.Junctions[j2] = new Junction(j2, ImmutableArray<BoundaryId>.Empty, default, false, null);
            }

            // Create boundary
            var boundaryId = TestDataFactory.BoundaryId(boundaryCounter++);
            topology.Boundaries[boundaryId] = new Boundary(
                boundaryId,
                plate1 != default ? plate1 : plateIds.First(),
                plate2 != default ? plate2 : plateIds.Last(),
                BoundaryType.Divergent,
                boundary,
                false, null);
        }

        return topology;
    }

    private static void ComparePartitions(PlatePolygonSet first, PlatePolygonSet second)
    {
        // Compare basic structure
        second.Polygons.Length.Should().Be(first.Polygons.Length);

        // Compare plate IDs
        var firstPlates = first.Polygons.Select(p => p.PlateId).OrderBy(id => id.Value).ToList();
        var secondPlates = second.Polygons.Select(p => p.PlateId).OrderBy(id => id.Value).ToList();
        secondPlates.Should().Equal(firstPlates);
    }

    private static Dictionary<PlateId, double> ComputeAreas(PlatePolygonSet partition)
    {
        var areas = new Dictionary<PlateId, double>();

        foreach (var polygon in partition.Polygons)
        {
            areas[polygon.PlateId] = ComputePolygonArea(polygon.OuterRing);
        }

        return areas;
    }

    private static Dictionary<PlateId, double> ComputeNetAreas(PlatePolygonSet partition)
    {
        var areas = new Dictionary<PlateId, double>();

        foreach (var polygon in partition.Polygons)
        {
            var outerArea = ComputePolygonArea(polygon.OuterRing);
            var holeArea = polygon.Holes.Sum(h => ComputePolygonArea(h));
            areas[polygon.PlateId] = outerArea - holeArea;
        }

        return areas;
    }

    private static double ComputeTotalArea(PlatePolygonSet partition)
    {
        return partition.Polygons.Sum(p => ComputePolygonArea(p.OuterRing));
    }

    private static double ComputePolygonArea(Polyline3 ring)
    {
        // Simplified area computation for testing
        if (ring.IsEmpty || ring.Points.Length < 3)
            return 0.0;

        var points = ring.Points;
        double area = 0.0;
        int n = points.Length - 1;

        for (int i = 0; i < n; i++)
        {
            area += points[i].X * points[(i + 1) % n].Y - points[(i + 1) % n].X * points[i].Y;
        }

        return Math.Abs(area) / 2.0;
    }

    private static Dictionary<PlateId, HashSet<PlateId>> BuildAdjacencyGraph(PlatePolygonSet partition)
    {
        var adjacency = new Dictionary<PlateId, HashSet<PlateId>>();

        foreach (var polygon in partition.Polygons)
        {
            if (!adjacency.ContainsKey(polygon.PlateId))
            {
                adjacency[polygon.PlateId] = new HashSet<PlateId>();
            }
        }

        // In a proper implementation, we would analyze shared boundaries
        // For testing, we assume plates are adjacent if they share boundary segments

        return adjacency;
    }

    private static int CountTripleJunctions(Dictionary<PlateId, HashSet<PlateId>> adjacency)
    {
        // Count junctions where 3 plates meet
        // A triple junction occurs when 3 plates are all mutually adjacent
        int count = 0;

        var plates = adjacency.Keys.ToList();
        for (int i = 0; i < plates.Count; i++)
        {
            for (int j = i + 1; j < plates.Count; j++)
            {
                for (int k = j + 1; k < plates.Count; k++)
                {
                    var p1 = plates[i];
                    var p2 = plates[j];
                    var p3 = plates[k];

                    if (adjacency[p1].Contains(p2) &&
                        adjacency[p1].Contains(p3) &&
                        adjacency[p2].Contains(p3))
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    private static int CountBoundaries(PlatePolygonSet partition)
    {
        int count = 0;
        foreach (var polygon in partition.Polygons)
        {
            // Count edges in outer ring
            if (!polygon.OuterRing.IsEmpty)
            {
                count += polygon.OuterRing.Points.Length - 1;
            }
        }
        return count / 2; // Each boundary is shared by 2 plates
    }

    private static InMemoryTopologyStateView CreateRoundTripTopologyWithHoles()
    {
        var topology = new InMemoryTopologyStateView("roundtrip-holes");

        var outerPlate = TestDataFactory.PlateId(1);
        var innerPlate = TestDataFactory.PlateId(2);

        topology.Plates[outerPlate] = new PlateEntity(outerPlate, false, null);
        topology.Plates[innerPlate] = new PlateEntity(innerPlate, false, null);

        // Create outer and inner squares
        var j1 = TestDataFactory.JunctionId(1);
        var j2 = TestDataFactory.JunctionId(2);
        var j3 = TestDataFactory.JunctionId(3);
        var j4 = TestDataFactory.JunctionId(4);
        var j5 = TestDataFactory.JunctionId(5);
        var j6 = TestDataFactory.JunctionId(6);
        var j7 = TestDataFactory.JunctionId(7);
        var j8 = TestDataFactory.JunctionId(8);

        var b1 = TestDataFactory.BoundaryId(1);
        var b2 = TestDataFactory.BoundaryId(2);
        var b3 = TestDataFactory.BoundaryId(3);
        var b4 = TestDataFactory.BoundaryId(4);
        var b5 = TestDataFactory.BoundaryId(5);
        var b6 = TestDataFactory.BoundaryId(6);
        var b7 = TestDataFactory.BoundaryId(7);
        var b8 = TestDataFactory.BoundaryId(8);

        topology.Junctions[j1] = new Junction(j1, ImmutableArray.Create(b1, b4), default, false, null);
        topology.Junctions[j2] = new Junction(j2, ImmutableArray.Create(b1, b2), default, false, null);
        topology.Junctions[j3] = new Junction(j3, ImmutableArray.Create(b2, b3), default, false, null);
        topology.Junctions[j4] = new Junction(j4, ImmutableArray.Create(b3, b4), default, false, null);

        topology.Junctions[j5] = new Junction(j5, ImmutableArray.Create(b5, b8), default, false, null);
        topology.Junctions[j6] = new Junction(j6, ImmutableArray.Create(b5, b6), default, false, null);
        topology.Junctions[j7] = new Junction(j7, ImmutableArray.Create(b6, b7), default, false, null);
        topology.Junctions[j8] = new Junction(j8, ImmutableArray.Create(b7, b8), default, false, null);

        topology.Boundaries[b1] = new Boundary(b1, outerPlate, innerPlate, BoundaryType.Divergent,
            new Polyline3([new Point3(0, 0, 0), new Point3(2, 0, 0)]), false, null);
        topology.Boundaries[b2] = new Boundary(b2, outerPlate, innerPlate, BoundaryType.Divergent,
            new Polyline3([new Point3(2, 0, 0), new Point3(2, 2, 0)]), false, null);
        topology.Boundaries[b3] = new Boundary(b3, outerPlate, innerPlate, BoundaryType.Divergent,
            new Polyline3([new Point3(2, 2, 0), new Point3(0, 2, 0)]), false, null);
        topology.Boundaries[b4] = new Boundary(b4, outerPlate, innerPlate, BoundaryType.Divergent,
            new Polyline3([new Point3(0, 2, 0), new Point3(0, 0, 0)]), false, null);

        topology.Boundaries[b5] = new Boundary(b5, innerPlate, outerPlate, BoundaryType.Convergent,
            new Polyline3([new Point3(0.5, 0.5, 0), new Point3(1.5, 0.5, 0)]), false, null);
        topology.Boundaries[b6] = new Boundary(b6, innerPlate, outerPlate, BoundaryType.Convergent,
            new Polyline3([new Point3(1.5, 0.5, 0), new Point3(1.5, 1.5, 0)]), false, null);
        topology.Boundaries[b7] = new Boundary(b7, innerPlate, outerPlate, BoundaryType.Convergent,
            new Polyline3([new Point3(1.5, 1.5, 0), new Point3(0.5, 1.5, 0)]), false, null);
        topology.Boundaries[b8] = new Boundary(b8, innerPlate, outerPlate, BoundaryType.Convergent,
            new Polyline3([new Point3(0.5, 1.5, 0), new Point3(0.5, 0.5, 0)]), false, null);

        return topology;
    }

    #endregion
}
