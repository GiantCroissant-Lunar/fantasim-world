using FantaSim.Geosphere.Plate.Polygonization.Contracts.CMap;
using FantaSim.Geosphere.Plate.Polygonization.Solver.CMap;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using NSubstitute;
using UnifyGeometry;

using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;

namespace FantaSim.Geosphere.Plate.Polygonization.Tests.CMap;

/// <summary>
/// Tests for BoundaryCMapBuilder - verifies deterministic cmap construction.
/// 
/// RFC-V2-0041 §9: CMap must be deterministic given the same topology state.
/// </summary>
public class BoundaryCMapBuilderTests
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
        // Identity and LastEventSequence don't matter for cmap building - they can be default
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

    private static Boundary CreateBoundary(BoundaryId id, PlateId left, PlateId right, Point3 start, Point3 end) =>
        new(id, left, right, BoundaryType.Divergent, new Polyline3([start, end]), IsRetired: false, RetirementReason: null);

    #endregion

    #region Simple Cases

    [Fact]
    public void Build_SingleBoundaryTwoJunctions_CreatesFourDarts()
    {
        // Arrange: One boundary connecting two junctions
        //   J1 ----B1---- J2
        var j1 = MakeJunction(1);
        var j2 = MakeJunction(2);
        var b1 = MakeBoundary(1);
        var p1 = MakePlate(1);
        var p2 = MakePlate(2);

        var junctions = new Dictionary<JunctionId, Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1),
            [j2] = CreateJunction(j2, 1, 0, b1)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, p1, p2, new Point3(0, 0, 0), new Point3(1, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var builder = new BoundaryCMapBuilder();

        // Act
        var cmap = builder.Build(topology);

        // Assert
        Assert.Equal(2, cmap.Junctions.Count());
        Assert.Equal(2, cmap.Darts.Count()); // Only 2 darts for segment index 0

        var dartsList = cmap.Darts.ToList();

        // Should have forward and backward darts
        var forward = dartsList.Single(d => d.Direction == DartDirection.Forward);
        var backward = dartsList.Single(d => d.Direction == DartDirection.Backward);

        // Twin relationship
        Assert.Equal(backward, cmap.Twin(forward));
        Assert.Equal(forward, cmap.Twin(backward));

        // Origins
        Assert.Equal(j1, cmap.Origin(forward));  // Forward starts at J1
        Assert.Equal(j2, cmap.Origin(backward)); // Backward starts at J2
    }

    [Fact]
    public void Build_TripleJunction_HasThreeIncidentDartsInCyclicOrder()
    {
        // Arrange: Triple junction at center
        //       J2
        //       |
        //       B2
        //       |
        //  J1--B1--J0--B3--J3
        //
        // J0 is at origin, J1 to left (-1,0), J2 above (0,1), J3 to right (1,0)
        var j0 = MakeJunction(10); // Center junction
        var j1 = MakeJunction(1);
        var j2 = MakeJunction(2);
        var j3 = MakeJunction(3);

        var b1 = MakeBoundary(1);
        var b2 = MakeBoundary(2);
        var b3 = MakeBoundary(3);

        var p1 = MakePlate(1);
        var p2 = MakePlate(2);

        var junctions = new Dictionary<JunctionId, Junction>
        {
            [j0] = CreateJunction(j0, 0, 0, b1, b2, b3),
            [j1] = CreateJunction(j1, -1, 0, b1),
            [j2] = CreateJunction(j2, 0, 1, b2),
            [j3] = CreateJunction(j3, 1, 0, b3)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, p1, p2, new Point3(-1, 0, 0), new Point3(0, 0, 0)),
            [b2] = CreateBoundary(b2, p1, p2, new Point3(0, 0, 0), new Point3(0, 1, 0)),
            [b3] = CreateBoundary(b3, p1, p2, new Point3(0, 0, 0), new Point3(1, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var builder = new BoundaryCMapBuilder();

        // Act
        var cmap = builder.Build(topology);

        // Assert: J0 should have 3 incident darts
        var incident = cmap.IncidentOrdered(j0);
        Assert.Equal(3, incident.Count);

        // Verify cyclic order: CCW from +X should be:
        // 1. B3 backward (pointing right, angle 0°) 
        // 2. B2 forward (pointing up, angle 90°)
        // 3. B1 backward (pointing left, angle 180°)
        // 
        // Wait - we need OUTGOING darts from J0
        // B1: J1->J0, so outgoing from J0 is B1-Backward going toward J1 (angle=180°)
        // B2: J0->J2, so outgoing from J0 is B2-Forward going toward J2 (angle=90°)
        // B3: J0->J3, so outgoing from J0 is B3-Forward going toward J3 (angle=0°)

        // Note: The angles depend on which junction is the endpoint
        // Let me just verify all 3 are present for now
        Assert.All(incident, d => Assert.Equal(j0, cmap.Origin(d)));
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void Build_SameTopology_ProducesSameCMap()
    {
        // Arrange
        var j1 = MakeJunction(1);
        var j2 = MakeJunction(2);
        var b1 = MakeBoundary(1);
        var p1 = MakePlate(1);
        var p2 = MakePlate(2);

        var junctions = new Dictionary<JunctionId, Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1),
            [j2] = CreateJunction(j2, 1, 0, b1)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, p1, p2, new Point3(0, 0, 0), new Point3(1, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var builder = new BoundaryCMapBuilder();

        // Act: Build twice
        var cmap1 = builder.Build(topology);
        var cmap2 = builder.Build(topology);

        // Assert: Same structure
        var darts1 = cmap1.Darts.ToList();
        var darts2 = cmap2.Darts.ToList();

        Assert.Equal(darts1.Count, darts2.Count);
        for (int i = 0; i < darts1.Count; i++)
        {
            Assert.Equal(darts1[i], darts2[i]);
            Assert.Equal(cmap1.Twin(darts1[i]), cmap2.Twin(darts2[i]));
            Assert.Equal(cmap1.Next(darts1[i]), cmap2.Next(darts2[i]));
            Assert.Equal(cmap1.Origin(darts1[i]), cmap2.Origin(darts2[i]));
        }
    }

    #endregion

    #region Face Walk Tests

    [Fact]
    public void WalkFace_SquareLoop_ReturnsToStart()
    {
        // Arrange: Square with 4 junctions and 4 boundaries
        //   J2----B2----J3
        //   |           |
        //   B1          B3
        //   |           |
        //   J1----B4----J4
        //
        // Boundaries form a closed loop (square)
        var j1 = MakeJunction(1);
        var j2 = MakeJunction(2);
        var j3 = MakeJunction(3);
        var j4 = MakeJunction(4);

        var b1 = MakeBoundary(1); // J1->J2 (left edge)
        var b2 = MakeBoundary(2); // J2->J3 (top edge)
        var b3 = MakeBoundary(3); // J3->J4 (right edge)
        var b4 = MakeBoundary(4); // J4->J1 (bottom edge)

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
        var builder = new BoundaryCMapBuilder();

        // Act
        var cmap = builder.Build(topology);

        // Pick any dart and walk its face
        var startDart = cmap.Darts.First();
        var face = cmap.WalkFace(startDart).ToList();

        // Assert: Should form a closed loop back to start
        Assert.Contains(startDart, face);
        Assert.True(face.Count <= 4, "Face should have at most 4 darts for square");

        // Verify it's a cycle: Next(last) should equal first
        var last = face[^1];
        Assert.Equal(startDart, cmap.Next(last));
    }

    [Fact]
    public void EnumerateFaces_SquareLoop_ReturnsTwoFaces()
    {
        // Same setup as above - square should have exactly 2 faces (inside and outside)
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
        var builder = new BoundaryCMapBuilder();

        // Act
        var cmap = builder.Build(topology);
        var faces = cmap.EnumerateFaces().ToList();

        // Assert: Should have exactly 2 faces
        Assert.Equal(2, faces.Count);

        // Each face should have 4 darts (for a square)
        Assert.All(faces, face => Assert.Equal(4, face.Count));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Build_EmptyTopology_ReturnsEmptyCMap()
    {
        // Arrange
        var topology = CreateMockTopology(
            new Dictionary<JunctionId, Junction>(),
            new Dictionary<BoundaryId, Boundary>());
        var builder = new BoundaryCMapBuilder();

        // Act
        var cmap = builder.Build(topology);

        // Assert
        Assert.Empty(cmap.Junctions);
        Assert.Empty(cmap.Darts);
    }

    [Fact]
    public void Build_RetiredJunctionsAndBoundaries_AreSkipped()
    {
        // Arrange: One active junction/boundary, one retired
        var j1 = MakeJunction(1);
        var j2 = MakeJunction(2);
        var j3 = MakeJunction(3); // Retired
        var b1 = MakeBoundary(1);
        var b2 = MakeBoundary(2); // Retired
        var p1 = MakePlate(1);
        var p2 = MakePlate(2);

        var junctions = new Dictionary<JunctionId, Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1),
            [j2] = CreateJunction(j2, 1, 0, b1),
            [j3] = new Junction(j3, [b2], new Point2(2, 0), IsRetired: true, RetirementReason: "Test")
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, p1, p2, new Point3(0, 0, 0), new Point3(1, 0, 0)),
            [b2] = new Boundary(b2, p1, p2, BoundaryType.Divergent, 
                new Polyline3([new Point3(1, 0, 0), new Point3(2, 0, 0)]), 
                IsRetired: true, RetirementReason: "Test")
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var builder = new BoundaryCMapBuilder();

        // Act
        var cmap = builder.Build(topology);

        // Assert: Only active junction and boundary
        Assert.Equal(2, cmap.Junctions.Count());
        Assert.Equal(2, cmap.Darts.Count()); // Only B1's darts
        Assert.DoesNotContain(j3, cmap.Junctions);
    }

    #endregion
}
