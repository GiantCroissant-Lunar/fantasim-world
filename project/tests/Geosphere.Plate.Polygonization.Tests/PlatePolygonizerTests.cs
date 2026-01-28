using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Polygonization.Solver;
using FantaSim.Geosphere.Plate.Polygonization.Solver.CMap;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using NSubstitute;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;

namespace FantaSim.Geosphere.Plate.Polygonization.Tests;

/// <summary>
/// Tests for PlatePolygonizer - verifies polygon extraction and face→plate attribution.
/// RFC-V2-0041 implementation tests.
/// </summary>
public class PlatePolygonizerTests
{
    #region Test Helpers

    private static IPlateTopologyStateView CreateMockTopology(
        Dictionary<JunctionId, Junction> junctions,
        Dictionary<BoundaryId, Boundary> boundaries,
        Dictionary<PlateId, PlateEntity>? plates = null)
    {
        var mock = Substitute.For<IPlateTopologyStateView>();
        mock.Junctions.Returns(junctions);
        mock.Boundaries.Returns(boundaries);
        mock.Plates.Returns(plates ?? new Dictionary<PlateId, PlateEntity>());
        mock.LastEventSequence.Returns(1);
        return mock;
    }

    private static JunctionId MakeJunction(int seed) =>
        new(new Guid(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

    private static BoundaryId MakeBoundary(int seed) =>
        new(new Guid(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

    private static PlateId MakePlate(int seed) =>
        new(new Guid(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

    private static Junction CreateJunction(JunctionId id, double x, double y, params BoundaryId[] boundaryIds) =>
        new(id, boundaryIds, new Point2(x, y), IsRetired: false, RetirementReason: null);

    private static Boundary CreateBoundary(
        BoundaryId id,
        PlateId leftPlate,
        PlateId rightPlate,
        Point3 start,
        Point3 end) =>
        new(id, leftPlate, rightPlate, BoundaryType.Divergent, new Polyline3([start, end]), IsRetired: false, RetirementReason: null);

    private static PlatePolygonizer CreatePolygonizer() =>
        new(new BoundaryCMapBuilder());

    private static readonly CanonicalTick TestTick = new(100);

    #endregion

    #region Square Loop - Basic Face Attribution

    /// <summary>
    /// Square loop with known Left/Right plates.
    /// 
    ///   J2----B2----J3
    ///   |           |
    ///   B1          B3
    ///   |           |
    ///   J1----B4----J4
    ///
    /// All boundaries have PlateIdLeft=P1, PlateIdRight=P2.
    /// 
    /// Face attribution uses "left side of directed darts" rule:
    /// - The face walking CCW around the square's exterior has darts pointing
    ///   in the same direction as boundaries, so LEFT = P1 → exterior = P1
    /// - The face walking CW around the square's interior has darts pointing
    ///   opposite to boundaries, so LEFT = P2 → interior = P2
    /// 
    /// After excluding the larger-area face (exterior), the interior polygon
    /// should be attributed to P2 (which is PlateIdRight of the boundaries).
    /// </summary>
    [Fact]
    public void PolygonizeAtTick_SquareLoop_AttributesInsideFaceToCorrectPlate()
    {
        // Arrange
        var j1 = MakeJunction(1);
        var j2 = MakeJunction(2);
        var j3 = MakeJunction(3);
        var j4 = MakeJunction(4);

        var b1 = MakeBoundary(1);
        var b2 = MakeBoundary(2);
        var b3 = MakeBoundary(3);
        var b4 = MakeBoundary(4);

        var plateOutside = MakePlate(1);  // On LEFT side of boundaries (exterior face)
        var plateInside = MakePlate(2);   // On RIGHT side of boundaries (interior face)

        // Define boundaries with consistent left/right plates
        // Walking CCW around square (J1→J2→J3→J4→J1):
        // Each boundary should have outside plate on left when walking CCW
        var junctions = new Dictionary<JunctionId, Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1, b4),
            [j2] = CreateJunction(j2, 0, 1, b1, b2),
            [j3] = CreateJunction(j3, 1, 1, b2, b3),
            [j4] = CreateJunction(j4, 1, 0, b3, b4)
        };

        // B1: J1→J2 (going up), left=outside, right=inside
        // B2: J2→J3 (going right), left=outside, right=inside
        // B3: J3→J4 (going down), left=outside, right=inside
        // B4: J4→J1 (going left), left=outside, right=inside
        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, plateOutside, plateInside, new Point3(0, 0, 0), new Point3(0, 1, 0)),
            [b2] = CreateBoundary(b2, plateOutside, plateInside, new Point3(0, 1, 0), new Point3(1, 1, 0)),
            [b3] = CreateBoundary(b3, plateOutside, plateInside, new Point3(1, 1, 0), new Point3(1, 0, 0)),
            [b4] = CreateBoundary(b4, plateOutside, plateInside, new Point3(1, 0, 0), new Point3(0, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var polygonizer = CreatePolygonizer();

        // Act
        var result = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Assert
        // Should have exactly 1 polygon (inside face; outside is excluded)
        Assert.Single(result.Polygons);

        var polygon = result.Polygons[0];
        // The inside face is on the RIGHT side of the directed boundaries
        Assert.Equal(plateInside, polygon.PlateId);

        // Ring should have 5 points (closed: first = last)
        Assert.Equal(5, polygon.OuterRing.Count);
        Assert.Empty(polygon.Holes);
    }

    #endregion

    #region Two Plates + Shared Boundary

    /// <summary>
    /// Two junctions with one boundary - tests that non-closed topologies
    /// result in no closed polygons (only a degenerate "face" that can't be
    /// consistently attributed).
    /// 
    /// In practice, real plate topologies should always be closed.
    /// </summary>
    [Fact]
    public void PolygonizeAtTick_OpenBoundary_AllowsPartialPolygonization()
    {
        // Arrange: Simple case - just two junctions with a boundary (not closed)
        var j1 = MakeJunction(1);
        var j2 = MakeJunction(2);

        var b1 = MakeBoundary(1);

        var plateA = MakePlate(1);
        var plateB = MakePlate(2);

        var junctions = new Dictionary<JunctionId, Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1),
            [j2] = CreateJunction(j2, 1, 0, b1)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, plateA, plateB, new Point3(0, 0, 0), new Point3(1, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var polygonizer = CreatePolygonizer();

        // Act - with AllowPartialPolygonization, this should not throw
        var options = new PolygonizationOptions(AllowPartialPolygonization: true);
        var result = polygonizer.PolygonizeAtTick(TestTick, topology, options);

        // Assert: With just a line segment and partial allowed, we get empty (no valid closed faces)
        Assert.Empty(result.Polygons);
    }

    #endregion

    #region Boundary Face Adjacency

    [Fact]
    public void GetBoundaryFaceAdjacency_ReturnsCorrectLeftRightPlates()
    {
        // Arrange: Simple two-junction, one-boundary
        var j1 = MakeJunction(1);
        var j2 = MakeJunction(2);
        var b1 = MakeBoundary(1);
        var plateLeft = MakePlate(1);
        var plateRight = MakePlate(2);

        var junctions = new Dictionary<JunctionId, Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1),
            [j2] = CreateJunction(j2, 1, 0, b1)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, plateLeft, plateRight, new Point3(0, 0, 0), new Point3(1, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var polygonizer = CreatePolygonizer();

        // Act
        var result = polygonizer.GetBoundaryFaceAdjacency(TestTick, topology);

        // Assert
        Assert.Single(result.Adjacencies);

        var adj = result.Adjacencies[0];
        Assert.Equal(b1, adj.BoundaryId);
        Assert.Equal(plateLeft, adj.LeftPlateId);
        Assert.Equal(plateRight, adj.RightPlateId);
    }

    #endregion

    #region Validation

    [Fact]
    public void Validate_ValidSquareTopology_ReturnsIsValidTrue()
    {
        // Arrange: Valid square loop
        var j1 = MakeJunction(1);
        var j2 = MakeJunction(2);
        var j3 = MakeJunction(3);
        var j4 = MakeJunction(4);

        var b1 = MakeBoundary(1);
        var b2 = MakeBoundary(2);
        var b3 = MakeBoundary(3);
        var b4 = MakeBoundary(4);

        var p1 = MakePlate(1);
        var p2 = MakePlate(2);

        var junctions = new Dictionary<JunctionId, Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1, b4),
            [j2] = CreateJunction(j2, 0, 1, b1, b2),
            [j3] = CreateJunction(j3, 1, 1, b2, b3),
            [j4] = CreateJunction(j4, 1, 0, b3, b4)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, p1, p2, new Point3(0, 0, 0), new Point3(0, 1, 0)),
            [b2] = CreateBoundary(b2, p1, p2, new Point3(0, 1, 0), new Point3(1, 1, 0)),
            [b3] = CreateBoundary(b3, p1, p2, new Point3(1, 1, 0), new Point3(1, 0, 0)),
            [b4] = CreateBoundary(b4, p1, p2, new Point3(1, 0, 0), new Point3(0, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var polygonizer = CreatePolygonizer();

        // Act
        var result = polygonizer.Validate(TestTick, topology);

        // Assert
        Assert.True(result.IsValid, $"Expected valid, but got errors: {string.Join(", ", result.OpenBoundaries.Select(o => o.Message))}");
    }

    [Fact]
    public void Validate_DanglingBoundary_ReturnsOpenBoundaryDiagnostic()
    {
        // Arrange: Boundary connected to only one junction
        var j1 = MakeJunction(1);
        var b1 = MakeBoundary(1);
        var p1 = MakePlate(1);
        var p2 = MakePlate(2);

        var junctions = new Dictionary<JunctionId, Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1)
            // No second junction!
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, p1, p2, new Point3(0, 0, 0), new Point3(1, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var polygonizer = CreatePolygonizer();

        // Act
        var result = polygonizer.Validate(TestTick, topology);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.OpenBoundaries);
        Assert.Equal(b1, result.OpenBoundaries[0].BoundaryId);
    }

    #endregion

    #region Determinism

    [Fact]
    public void PolygonizeAtTick_SameTopology_ProducesSameResult()
    {
        // Arrange
        var j1 = MakeJunction(1);
        var j2 = MakeJunction(2);
        var j3 = MakeJunction(3);
        var j4 = MakeJunction(4);

        var b1 = MakeBoundary(1);
        var b2 = MakeBoundary(2);
        var b3 = MakeBoundary(3);
        var b4 = MakeBoundary(4);

        var p1 = MakePlate(1);
        var p2 = MakePlate(2);

        var junctions = new Dictionary<JunctionId, Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1, b4),
            [j2] = CreateJunction(j2, 0, 1, b1, b2),
            [j3] = CreateJunction(j3, 1, 1, b2, b3),
            [j4] = CreateJunction(j4, 1, 0, b3, b4)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, p1, p2, new Point3(0, 0, 0), new Point3(0, 1, 0)),
            [b2] = CreateBoundary(b2, p1, p2, new Point3(0, 1, 0), new Point3(1, 1, 0)),
            [b3] = CreateBoundary(b3, p1, p2, new Point3(1, 1, 0), new Point3(1, 0, 0)),
            [b4] = CreateBoundary(b4, p1, p2, new Point3(1, 0, 0), new Point3(0, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var polygonizer = CreatePolygonizer();

        // Act
        var result1 = polygonizer.PolygonizeAtTick(TestTick, topology);
        var result2 = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Assert
        Assert.Equal(result1.Polygons.Length, result2.Polygons.Length);
        for (int i = 0; i < result1.Polygons.Length; i++)
        {
            Assert.Equal(result1.Polygons[i].PlateId, result2.Polygons[i].PlateId);
            Assert.Equal(result1.Polygons[i].OuterRing.Count, result2.Polygons[i].OuterRing.Count);
        }
    }

    #endregion

    #region Empty / Edge Cases

    [Fact]
    public void PolygonizeAtTick_EmptyTopology_ReturnsEmptyPolygonSet()
    {
        // Arrange
        var topology = CreateMockTopology(
            new Dictionary<JunctionId, Junction>(),
            new Dictionary<BoundaryId, Boundary>());
        var polygonizer = CreatePolygonizer();

        // Act
        var result = polygonizer.PolygonizeAtTick(TestTick, topology);

        // Assert
        Assert.Empty(result.Polygons);
        Assert.Equal(TestTick, result.Tick);
    }

    #endregion
}
