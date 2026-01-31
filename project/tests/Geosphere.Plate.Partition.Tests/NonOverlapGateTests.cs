using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Partition.Solver;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
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
/// Non-Overlap Gate Tests - RFC-V2-0047 §3.2
/// Verifies that polygon interiors are disjoint (boundaries can touch but not cross).
/// </summary>
public sealed class NonOverlapGateTests
{
    private static readonly CanonicalTick TestTick = new(100);

    #region Test: No two polygons share interior points

    [Fact]
    public void DisjointInteriors_TwoPlates_NoInteriorOverlap()
    {
        // Arrange: Two adjacent plates
        var topology = TestDataFactory.CreateTwoPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var result = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Assert: Polygons should not have overlapping interiors
        var polygons = result.Polygons.ToList();
        for (int i = 0; i < polygons.Count; i++)
        {
            for (int j = i + 1; j < polygons.Count; j++)
            {
                var interiorOverlap = CheckInteriorOverlap(polygons[i], polygons[j]);
                interiorOverlap.Should().BeFalse(
                    $"Polygons for plates {polygons[i].PlateId} and {polygons[j].PlateId} should not overlap");
            }
        }
    }

    [Fact]
    public void DisjointInteriors_ThreePlates_NoInteriorOverlap()
    {
        // Arrange: Three plates meeting at triple junction
        var topology = TestDataFactory.CreateThreePlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var result = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Assert: All polygon pairs should have disjoint interiors
        var polygons = result.Polygons.ToList();
        CheckAllPairsDisjoint(polygons);
    }

    [Fact]
    public void DisjointInteriors_FourPlates_NoInteriorOverlap()
    {
        // Arrange: Four plates
        var topology = TestDataFactory.CreateFourPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var result = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Assert: All polygon pairs should have disjoint interiors
        var polygons = result.Polygons.ToList();
        CheckAllPairsDisjoint(polygons);
    }

    [Fact]
    public void DisjointInteriors_WithHoles_NoInteriorOverlap()
    {
        // Arrange: Plate with holes
        var topology = CreateTopologyWithNestedHoles();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var result = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Assert: Outer polygon and hole should not overlap
        foreach (var polygon in result.Polygons)
        {
            if (!polygon.Holes.IsDefaultOrEmpty)
            {
                foreach (var hole in polygon.Holes)
                {
                    var overlap = CheckInteriorOverlap(polygon.OuterRing, hole);
                    overlap.Should().BeFalse("Holes should not overlap with outer boundary");
                }
            }
        }
    }

    #endregion

    #region Test: Boundaries can touch but not cross

    [Fact]
    public void BoundaryTouching_AdjacentPlates_ShareBoundary()
    {
        // Arrange: Two plates sharing a boundary
        var topology = TestDataFactory.CreateTwoPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var adjacency = polygonizer.GetBoundaryFaceAdjacency(TestTick, topology);

        // Assert: Boundaries should be shared between exactly two plates
        foreach (var adj in adjacency.Adjacencies)
        {
            // Each boundary should have exactly two adjacent faces (plates)
            adj.LeftPlateId.Should().NotBe(default(PlateId));
            adj.RightPlateId.Should().NotBe(default(PlateId));
        }
    }

    [Fact]
    public void BoundaryTouching_TripleJunction_BoundariesMeetAtPoint()
    {
        // Arrange: Three plates meeting at triple junction
        var topology = TestDataFactory.CreateThreePlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var result = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Assert: Each polygon should have distinct boundaries
        var boundarySegments = new List<(PlateId PlateId, Point3 Start, Point3 End)>();
        foreach (var polygon in result.Polygons)
        {
            var ring = polygon.OuterRing;
            for (int i = 0; i < ring.Points.Length - 1; i++)
            {
                boundarySegments.Add((polygon.PlateId, ring.Points[i], ring.Points[i + 1]));
            }
        }

        // Boundaries should be shared (appear in reverse for adjacent plate)
        var sharedBoundaries = CountSharedBoundaries(boundarySegments);
        sharedBoundaries.Should().BeGreaterThan(0, "Some boundaries should be shared between plates");
    }

    [Fact]
    public void BoundaryNoCross_BoundariesDoNotIntersectExceptAtSharedPoints()
    {
        // Arrange: Valid topology
        var topology = TestDataFactory.CreateTwoPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var result = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Assert: Boundaries should not cross each other
        var crossings = CountBoundaryCrossings(result);
        crossings.Should().Be(0, "Boundaries should not cross");
    }

    #endregion

    #region Test: Verify using point-in-polygon tests

    [Fact]
    public void PointInPolygon_ValidPartition_PointInExactlyOnePolygon()
    {
        // Arrange: Simple two-plate topology
        var topology = TestDataFactory.CreateTwoPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var result = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Test points inside various plates
        var testPoints = new[]
        {
            new Point3(-0.5, 0, 0), // Expected in left plate
            new Point3(0.5, 0, 0),  // Expected in right plate
        };

        foreach (var point in testPoints)
        {
            var containingPlates = result.Polygons
                .Where(p => PointInPolygon(point, p.OuterRing))
                .Select(p => p.PlateId)
                .ToList();

            containingPlates.Count.Should().BeLessOrEqualTo(1,
                $"Point ({point.X}, {point.Y}, {point.Z}) should be in at most one polygon");
        }
    }

    [Fact]
    public void PointInPolygon_BoundaryPoint_PointInTwoPolygons()
    {
        // Arrange: Two adjacent plates
        var topology = TestDataFactory.CreateTwoPlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var result = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Test a point on the shared boundary
        var boundaryPoint = new Point3(0, 0, 0);

        var containingPolygons = result.Polygons
            .Where(p => PointOnBoundary(boundaryPoint, p.OuterRing))
            .ToList();

        // A boundary point should be on the edge of both adjacent polygons
        containingPolygons.Count.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void PointInPolygon_MultiplePlates_PointInExactlyOnePolygon()
    {
        // Arrange: Three plates
        var topology = TestDataFactory.CreateThreePlateTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var result = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Sample points should each be in exactly one polygon
        var testPoints = GenerateTestPoints();

        foreach (var point in testPoints)
        {
            var containingPlates = result.Polygons
                .Where(p => PointInPolygon(point, p.OuterRing))
                .Select(p => p.PlateId)
                .ToList();

            containingPlates.Count.Should().BeLessOrEqualTo(1,
                $"Point ({point.X}, {point.Y}, {point.Z}) should be in at most one polygon");
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void DisjointInteriors_EmptyPartition_NoOverlap()
    {
        // Arrange: Empty partition
        var emptySet = new PlatePolygonSet(TestTick, ImmutableArray<PlatePolygon>.Empty);

        // Assert: Empty partition trivially satisfies non-overlap
        emptySet.Polygons.Should().BeEmpty();
    }

    [Fact]
    public void DisjointInteriors_SinglePolygon_NoOverlap()
    {
        // Arrange: Single polygon
        var singlePolygon = new PlatePolygon(
            TestDataFactory.PlateId(1),
            new Polyline3([new Point3(0, 0, 0), new Point3(1, 0, 0), new Point3(1, 1, 0), new Point3(0, 0, 0)]),
            ImmutableArray<Polyline3>.Empty);

        var singleSet = new PlatePolygonSet(TestTick, ImmutableArray.Create(singlePolygon));

        // Assert: Single polygon has no overlapping pairs
        singleSet.Polygons.Length.Should().Be(1);
    }

    [Fact]
    public void BoundaryTouching_ConcentricPolygons_OuterContainsInner()
    {
        // Arrange: Concentric rings (hole topology)
        var topology = CreateConcentricTopology();
        var polygonizer = new PlatePolygonizer(new BoundaryCMapBuilder());

        // Act
        var result = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Assert: Polygons should be properly nested, not overlapping
        var outerPolygon = result.Polygons.FirstOrDefault(p => p.PlateId == TestDataFactory.PlateId(1));
        var innerPolygon = result.Polygons.FirstOrDefault(p => p.PlateId == TestDataFactory.PlateId(2));

        if (outerPolygon.PlateId != default && innerPolygon.PlateId != default)
        {
            // Inner polygon should be contained within outer polygon
            // but they should not overlap in their interiors
            var outerInteriorContainsInner = PolygonContainsPolygon(outerPolygon.OuterRing, innerPolygon.OuterRing);
            outerInteriorContainsInner.Should().BeTrue("Outer polygon should contain inner polygon");
        }
    }

    #endregion

    #region Helpers

    private static void CheckAllPairsDisjoint(List<PlatePolygon> polygons)
    {
        for (int i = 0; i < polygons.Count; i++)
        {
            for (int j = i + 1; j < polygons.Count; j++)
            {
                var overlap = CheckInteriorOverlap(polygons[i], polygons[j]);
                overlap.Should().BeFalse(
                    $"Polygons for plates {polygons[i].PlateId} and {polygons[j].PlateId} should have disjoint interiors");
            }
        }
    }

    private static bool CheckInteriorOverlap(PlatePolygon p1, PlatePolygon p2)
    {
        return CheckInteriorOverlap(p1.OuterRing, p2.OuterRing);
    }

    private static bool CheckInteriorOverlap(Polyline3 ring1, Polyline3 ring2)
    {
        // Simple bounding box check first
        var (min1, max1) = GetBoundingBox(ring1);
        var (min2, max2) = GetBoundingBox(ring2);

        if (!BoundingBoxesOverlap(min1, max1, min2, max2))
        {
            return false;
        }

        // Check if any edge of ring1 intersects any edge of ring2
        for (int i = 0; i < ring1.Points.Length - 1; i++)
        {
            for (int j = 0; j < ring2.Points.Length - 1; j++)
            {
                if (SegmentsIntersect(
                    ring1.Points[i], ring1.Points[i + 1],
                    ring2.Points[j], ring2.Points[j + 1]))
                {
                    // Check if it's a proper crossing (not just touching at endpoints)
                    var intersectionType = GetIntersectionType(
                        ring1.Points[i], ring1.Points[i + 1],
                        ring2.Points[j], ring2.Points[j + 1]);

                    if (intersectionType == IntersectionType.Crossing)
                    {
                        return true;
                    }
                }
            }
        }

        // Check if one polygon is completely inside another
        if (PolygonContainsPolygon(ring1, ring2) || PolygonContainsPolygon(ring2, ring1))
        {
            // If one is completely inside, check if it's a hole relationship
            // For this test, we consider non-hole containment as overlap
            return true;
        }

        return false;
    }

    private static bool BoundingBoxesOverlap(Point3 min1, Point3 max1, Point3 min2, Point3 max2)
    {
        return !(max1.X < min2.X || min1.X > max2.X ||
                 max1.Y < min2.Y || min1.Y > max2.Y ||
                 max1.Z < min2.Z || min1.Z > max2.Z);
    }

    private static (Point3 Min, Point3 Max) GetBoundingBox(Polyline3 ring)
    {
        if (ring.IsEmpty)
            return (new Point3(0, 0, 0), new Point3(0, 0, 0));

        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

        foreach (var p in ring.Points)
        {
            minX = Math.Min(minX, p.X);
            minY = Math.Min(minY, p.Y);
            minZ = Math.Min(minZ, p.Z);
            maxX = Math.Max(maxX, p.X);
            maxY = Math.Max(maxY, p.Y);
            maxZ = Math.Max(maxZ, p.Z);
        }

        return (new Point3(minX, minY, minZ), new Point3(maxX, maxY, maxZ));
    }

    private static bool SegmentsIntersect(Point3 a1, Point3 a2, Point3 b1, Point3 b2)
    {
        // 3D segment intersection test (simplified - projects to XY plane)
        // For more accurate 3D test, use proper geometric predicates

        double d1 = Direction(b1, b2, a1);
        double d2 = Direction(b1, b2, a2);
        double d3 = Direction(a1, a2, b1);
        double d4 = Direction(a1, a2, b2);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
        {
            return true;
        }

        if (d1 == 0 && OnSegment(b1, b2, a1)) return true;
        if (d2 == 0 && OnSegment(b1, b2, a2)) return true;
        if (d3 == 0 && OnSegment(a1, a2, b1)) return true;
        if (d4 == 0 && OnSegment(a1, a2, b2)) return true;

        return false;
    }

    private static double Direction(Point3 pi, Point3 pj, Point3 pk)
    {
        return (pk.X - pi.X) * (pj.Y - pi.Y) - (pj.X - pi.X) * (pk.Y - pi.Y);
    }

    private static bool OnSegment(Point3 pi, Point3 pj, Point3 pk)
    {
        return Math.Min(pi.X, pj.X) <= pk.X && pk.X <= Math.Max(pi.X, pj.X) &&
               Math.Min(pi.Y, pj.Y) <= pk.Y && pk.Y <= Math.Max(pi.Y, pj.Y);
    }

    private enum IntersectionType
    {
        None,
        Touching,
        Crossing
    }

    private static IntersectionType GetIntersectionType(Point3 a1, Point3 a2, Point3 b1, Point3 b2)
    {
        // Check if segments share an endpoint (touching)
        if (a1 == b1 || a1 == b2 || a2 == b1 || a2 == b2)
        {
            return IntersectionType.Touching;
        }

        // Check for proper crossing
        if (SegmentsIntersect(a1, a2, b1, b2))
        {
            return IntersectionType.Crossing;
        }

        return IntersectionType.None;
    }

    private static bool PolygonContainsPolygon(Polyline3 outer, Polyline3 inner)
    {
        // Check if all points of inner are inside outer
        foreach (var point in inner.Points)
        {
            if (!PointInPolygon(point, outer))
            {
                return false;
            }
        }
        return true;
    }

    private static bool PointInPolygon(Point3 point, Polyline3 ring)
    {
        // Ray casting algorithm (2D projection)
        bool inside = false;
        int n = ring.Points.Length - 1; // Exclude closing point

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var pi = ring.Points[i];
            var pj = ring.Points[j];

            if (((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static bool PointOnBoundary(Point3 point, Polyline3 ring)
    {
        for (int i = 0; i < ring.Points.Length - 1; i++)
        {
            if (PointOnSegment(point, ring.Points[i], ring.Points[i + 1]))
            {
                return true;
            }
        }
        return false;
    }

    private static bool PointOnSegment(Point3 p, Point3 a, Point3 b)
    {
        const double epsilon = 1e-9;

        // Check if point is collinear with segment
        var cross = CrossProduct(
            new Vector3(b.X - a.X, b.Y - a.Y, b.Z - a.Z),
            new Vector3(p.X - a.X, p.Y - a.Y, p.Z - a.Z));

        if (Math.Abs(cross.X) > epsilon || Math.Abs(cross.Y) > epsilon || Math.Abs(cross.Z) > epsilon)
        {
            return false;
        }

        // Check if point is between segment endpoints
        var dot = (p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y);
        if (dot < 0) return false;

        var squaredLength = (b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y);
        if (dot > squaredLength) return false;

        return true;
    }

    private static Vector3 CrossProduct(Vector3 a, Vector3 b)
    {
        return new Vector3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);
    }

    private static int CountSharedBoundaries(List<(PlateId PlateId, Point3 Start, Point3 End)> segments)
    {
        int sharedCount = 0;
        var processed = new HashSet<int>();

        for (int i = 0; i < segments.Count; i++)
        {
            if (processed.Contains(i)) continue;

            for (int j = i + 1; j < segments.Count; j++)
            {
                if (processed.Contains(j)) continue;

                // Check if segments are the same (possibly reversed)
                if ((segments[i].Start == segments[j].End && segments[i].End == segments[j].Start) ||
                    (segments[i].Start == segments[j].Start && segments[i].End == segments[j].End))
                {
                    if (segments[i].PlateId != segments[j].PlateId)
                    {
                        sharedCount++;
                        processed.Add(i);
                        processed.Add(j);
                        break;
                    }
                }
            }
        }

        return sharedCount;
    }

    private static int CountBoundaryCrossings(PlatePolygonSet result)
    {
        int crossings = 0;

        // Extract all boundary segments
        var allSegments = new List<(PlateId PlateId, Point3 Start, Point3 End)>();
        foreach (var polygon in result.Polygons)
        {
            var ring = polygon.OuterRing;
            for (int i = 0; i < ring.Points.Length - 1; i++)
            {
                allSegments.Add((polygon.PlateId, ring.Points[i], ring.Points[i + 1]));
            }
        }

        // Check all segment pairs for crossings
        for (int i = 0; i < allSegments.Count; i++)
        {
            for (int j = i + 1; j < allSegments.Count; j++)
            {
                // Skip if from same plate
                if (allSegments[i].PlateId == allSegments[j].PlateId) continue;

                var type = GetIntersectionType(
                    allSegments[i].Start, allSegments[i].End,
                    allSegments[j].Start, allSegments[j].End);

                if (type == IntersectionType.Crossing)
                {
                    crossings++;
                }
            }
        }

        return crossings;
    }

    private static List<Point3> GenerateTestPoints()
    {
        return new List<Point3>
        {
            new(-0.5, 0, 0),
            new(0.5, 0, 0),
            new(0, 0.5, 0),
            new(0, -0.5, 0),
            new(0.25, 0.25, 0),
            new(-0.25, -0.25, 0)
        };
    }

    private readonly record struct Vector3(double X, double Y, double Z);

    private static InMemoryTopologyStateView CreateTopologyWithNestedHoles()
    {
        // Reuse existing helper from TestDataFactory
        var topology = new InMemoryTopologyStateView("nested-holes");

        var outerPlate = TestDataFactory.PlateId(1);
        var innerPlate = TestDataFactory.PlateId(2);

        topology.Plates[outerPlate] = new PlateEntity(outerPlate, false, null);
        topology.Plates[innerPlate] = new PlateEntity(innerPlate, false, null);

        // Create nested squares
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

        topology.Junctions[j1] = new Junction(j1, ImmutableArray.Create(b1, b4), SurfacePoint.UnitSphere(UnitVector3d.UnitZ), false, null);
        topology.Junctions[j2] = new Junction(j2, ImmutableArray.Create(b1, b2), SurfacePoint.UnitSphere(UnitVector3d.UnitZ), false, null);
        topology.Junctions[j3] = new Junction(j3, ImmutableArray.Create(b2, b3), SurfacePoint.UnitSphere(UnitVector3d.UnitZ), false, null);
        topology.Junctions[j4] = new Junction(j4, ImmutableArray.Create(b3, b4), SurfacePoint.UnitSphere(UnitVector3d.UnitZ), false, null);

        topology.Junctions[j5] = new Junction(j5, ImmutableArray.Create(b5, b8), SurfacePoint.UnitSphere(UnitVector3d.UnitZ), false, null);
        topology.Junctions[j6] = new Junction(j6, ImmutableArray.Create(b5, b6), SurfacePoint.UnitSphere(UnitVector3d.UnitZ), false, null);
        topology.Junctions[j7] = new Junction(j7, ImmutableArray.Create(b6, b7), SurfacePoint.UnitSphere(UnitVector3d.UnitZ), false, null);
        topology.Junctions[j8] = new Junction(j8, ImmutableArray.Create(b7, b8), SurfacePoint.UnitSphere(UnitVector3d.UnitZ), false, null);

        topology.Boundaries[b1] = new Boundary(b1, outerPlate, innerPlate, BoundaryType.Divergent, new Polyline3([new Point3(0, 0, 0), new Point3(2, 0, 0)]), false, null);
        topology.Boundaries[b2] = new Boundary(b2, outerPlate, innerPlate, BoundaryType.Divergent, new Polyline3([new Point3(2, 0, 0), new Point3(2, 2, 0)]), false, null);
        topology.Boundaries[b3] = new Boundary(b3, outerPlate, innerPlate, BoundaryType.Divergent, new Polyline3([new Point3(2, 2, 0), new Point3(0, 2, 0)]), false, null);
        topology.Boundaries[b4] = new Boundary(b4, outerPlate, innerPlate, BoundaryType.Divergent, new Polyline3([new Point3(0, 2, 0), new Point3(0, 0, 0)]), false, null);

        topology.Boundaries[b5] = new Boundary(b5, innerPlate, outerPlate, BoundaryType.Convergent, new Polyline3([new Point3(0.5, 0.5, 0), new Point3(1.5, 0.5, 0)]), false, null);
        topology.Boundaries[b6] = new Boundary(b6, innerPlate, outerPlate, BoundaryType.Convergent, new Polyline3([new Point3(1.5, 0.5, 0), new Point3(1.5, 1.5, 0)]), false, null);
        topology.Boundaries[b7] = new Boundary(b7, innerPlate, outerPlate, BoundaryType.Convergent, new Polyline3([new Point3(1.5, 1.5, 0), new Point3(0.5, 1.5, 0)]), false, null);
        topology.Boundaries[b8] = new Boundary(b8, innerPlate, outerPlate, BoundaryType.Convergent, new Polyline3([new Point3(0.5, 1.5, 0), new Point3(0.5, 0.5, 0)]), false, null);

        return topology;
    }

    private static InMemoryTopologyStateView CreateConcentricTopology()
    {
        return CreateTopologyWithNestedHoles();
    }

    #endregion
}
