using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Partition.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using PlatePolygon = FantaSim.Geosphere.Plate.Partition.Contracts.PlatePolygon;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

using PlateEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Plate;
using BoundaryEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Boundary;
using JunctionEntity = FantaSim.Geosphere.Plate.Topology.Contracts.Entities.Junction;

namespace FantaSim.Geosphere.Plate.Partition.Tests;

/// <summary>
/// Factory for creating test data scenarios for partition tests.
/// Provides builders for various topology configurations.
/// </summary>
public static class TestDataFactory
{
    #region Plate IDs

    public static PlateId PlateId(int seed) =>
        new(new Guid(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

    public static BoundaryId BoundaryId(int seed) =>
        new(new Guid(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

    public static JunctionId JunctionId(int seed) =>
        new(new Guid(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

    #endregion

    #region Simple Topologies

    /// <summary>
    /// Creates a simple 2-plate topology: a square loop where plateA is outside
    /// and plateB is inside. This follows the CMap left/right rule.
    ///
    ///   J2----B2----J3
    ///   |           |
    ///   B1    [B]   B3    [A = outside]
    ///   |           |
    ///   J1----B4----J4
    ///
    /// All boundaries have Left=plateA (outside), Right=plateB (inside).
    /// Junction positions must match boundary geometry endpoints for correct CMap building.
    /// </summary>
    public static InMemoryTopologyStateView CreateTwoPlateTopology()
    {
        var topology = new InMemoryTopologyStateView("two-plate");

        var plateA = PlateId(1);  // Outside plate
        var plateB = PlateId(2);  // Inside plate

        var j1 = JunctionId(1);
        var j2 = JunctionId(2);
        var j3 = JunctionId(3);
        var j4 = JunctionId(4);

        var b1 = BoundaryId(1);
        var b2 = BoundaryId(2);
        var b3 = BoundaryId(3);
        var b4 = BoundaryId(4);

        topology.Plates[plateA] = new PlateEntity(plateA, false, null);
        topology.Plates[plateB] = new PlateEntity(plateB, false, null);

        // Junctions at each corner of a unit square
        // CRITICAL: Junction positions must match boundary geometry endpoints!
        // Using normalized (x, y, 0) vectors projected onto unit sphere
        topology.Junctions[j1] = new JunctionEntity(j1,
            ImmutableArray.Create(b1, b4),
            SurfacePoint.UnitSphere(UnitVector3d.UnitZ),  // Origin fallback
            false, null);
        topology.Junctions[j2] = new JunctionEntity(j2,
            ImmutableArray.Create(b1, b2),
            SurfacePoint.UnitSphere(UnitVector3d.UnitY),  // (0, 1, 0)
            false, null);
        topology.Junctions[j3] = new JunctionEntity(j3,
            ImmutableArray.Create(b2, b3),
            SurfacePoint.UnitSphere(UnitVector3d.FromComponents(1, 1, 0) ?? UnitVector3d.UnitZ),  // normalized (1,1,0)
            false, null);
        topology.Junctions[j4] = new JunctionEntity(j4,
            ImmutableArray.Create(b3, b4),
            SurfacePoint.UnitSphere(UnitVector3d.UnitX),  // (1, 0, 0)
            false, null);

        // Boundaries form a CCW loop: J1→J2→J3→J4→J1
        // All have Left=plateA (outside), Right=plateB (inside)
        // Geometry endpoints must match junction positions (mapped to 2D plane z=0)
        topology.Boundaries[b1] = new BoundaryEntity(b1, plateA, plateB,
            BoundaryType.Divergent,
            new Polyline3([new Point3(0, 0, 0), new Point3(0, 1, 0)]),
            false, null);
        topology.Boundaries[b2] = new BoundaryEntity(b2, plateA, plateB,
            BoundaryType.Convergent,
            new Polyline3([new Point3(0, 1, 0), new Point3(1, 1, 0)]),
            false, null);
        topology.Boundaries[b3] = new BoundaryEntity(b3, plateA, plateB,
            BoundaryType.Divergent,
            new Polyline3([new Point3(1, 1, 0), new Point3(1, 0, 0)]),
            false, null);
        topology.Boundaries[b4] = new BoundaryEntity(b4, plateA, plateB,
            BoundaryType.Convergent,
            new Polyline3([new Point3(1, 0, 0), new Point3(0, 0, 0)]),
            false, null);

        return topology;
    }

    /// <summary>
    /// Creates a 3-plate topology: a "Mercedes star" pattern where three plates
    /// (A, B, C) meet at a triple junction in the center, surrounded by an
    /// outer "world" plate.
    ///
    ///         J3
    ///        / \
    ///       B3  B6
    ///      /     \
    ///    J0--B1--J1
    ///      \     /
    ///       B2  B5
    ///        \ /
    ///         J2
    ///
    /// Interior: A occupies "top" slice, B occupies "bottom-left", C occupies "bottom-right"
    /// Exterior: worldPlate wraps around
    ///
    /// Boundary rules:
    /// - B1 (center→J1): Left=A, Right=C
    /// - B2 (center→J2): Left=C, Right=B
    /// - B3 (center→J3): Left=B, Right=A
    /// - B4 (J1→J2): Left=world, Right=C
    /// - B5 (J2→J3): Left=world, Right=B
    /// - B6 (J3→J1): Left=world, Right=A
    /// </summary>
    public static InMemoryTopologyStateView CreateThreePlateTopology()
    {
        var topology = new InMemoryTopologyStateView("three-plate");

        var plateA = PlateId(1);  // Top
        var plateB = PlateId(2);  // Bottom-left
        var plateC = PlateId(3);  // Bottom-right
        var world = PlateId(99);  // Outer world

        var center = JunctionId(1);  // Triple junction at origin
        var j1 = JunctionId(2);      // Right
        var j2 = JunctionId(3);      // Bottom
        var j3 = JunctionId(4);      // Top-left

        var b1 = BoundaryId(1);  // center→j1, A|C
        var b2 = BoundaryId(2);  // center→j2, C|B
        var b3 = BoundaryId(3);  // center→j3, B|A
        var b4 = BoundaryId(4);  // j1→j2, world|C
        var b5 = BoundaryId(5);  // j2→j3, world|B
        var b6 = BoundaryId(6);  // j3→j1, world|A

        topology.Plates[plateA] = new PlateEntity(plateA, false, null);
        topology.Plates[plateB] = new PlateEntity(plateB, false, null);
        topology.Plates[plateC] = new PlateEntity(plateC, false, null);
        topology.Plates[world] = new PlateEntity(world, false, null);

        // Junctions
        topology.Junctions[center] = new JunctionEntity(center,
            ImmutableArray.Create(b1, b2, b3),
            SurfacePoint.UnitSphere(UnitVector3d.FromComponents(0, 0, 1) ?? UnitVector3d.UnitZ),
            false, null);
        topology.Junctions[j1] = new JunctionEntity(j1,
            ImmutableArray.Create(b1, b4, b6),
            SurfacePoint.UnitSphere(UnitVector3d.FromComponents(1, 0, 1) ?? UnitVector3d.UnitZ),
            false, null);
        topology.Junctions[j2] = new JunctionEntity(j2,
            ImmutableArray.Create(b2, b4, b5),
            SurfacePoint.UnitSphere(UnitVector3d.FromComponents(0, -1, 1) ?? UnitVector3d.UnitZ),
            false, null);
        topology.Junctions[j3] = new JunctionEntity(j3,
            ImmutableArray.Create(b3, b5, b6),
            SurfacePoint.UnitSphere(UnitVector3d.FromComponents(-1, 1, 1) ?? UnitVector3d.UnitZ),
            false, null);

        // Internal boundaries from center
        topology.Boundaries[b1] = new BoundaryEntity(b1, plateA, plateC,
            BoundaryType.Divergent,
            new Polyline3([new Point3(0, 0, 0), new Point3(1, 0, 0)]),
            false, null);
        topology.Boundaries[b2] = new BoundaryEntity(b2, plateC, plateB,
            BoundaryType.Divergent,
            new Polyline3([new Point3(0, 0, 0), new Point3(0, -1, 0)]),
            false, null);
        topology.Boundaries[b3] = new BoundaryEntity(b3, plateB, plateA,
            BoundaryType.Divergent,
            new Polyline3([new Point3(0, 0, 0), new Point3(-1, 1, 0)]),
            false, null);

        // External boundaries forming outer triangle
        topology.Boundaries[b4] = new BoundaryEntity(b4, world, plateC,
            BoundaryType.Convergent,
            new Polyline3([new Point3(1, 0, 0), new Point3(0, -1, 0)]),
            false, null);
        topology.Boundaries[b5] = new BoundaryEntity(b5, world, plateB,
            BoundaryType.Convergent,
            new Polyline3([new Point3(0, -1, 0), new Point3(-1, 1, 0)]),
            false, null);
        topology.Boundaries[b6] = new BoundaryEntity(b6, world, plateA,
            BoundaryType.Convergent,
            new Polyline3([new Point3(-1, 1, 0), new Point3(1, 0, 0)]),
            false, null);

        return topology;
    }

    /// <summary>
    /// Creates a 4-plate topology: quadrants around a center junction,
    /// with an outer world plate.
    ///
    ///       J5---B8---J1
    ///       |    N    |
    ///       B7   B1   B5
    ///       |         |
    ///  J4---B4--center--B2---J2
    ///       |         |
    ///       B6   B3   B9
    ///       |    S    |
    ///       J3---B10--J6
    ///
    /// Actually simpler: 4-way junction at center, 4 corners.
    /// </summary>
    public static InMemoryTopologyStateView CreateFourPlateTopology()
    {
        var topology = new InMemoryTopologyStateView("four-plate");

        var north = PlateId(1);
        var east = PlateId(2);
        var south = PlateId(3);
        var west = PlateId(4);
        var world = PlateId(99);

        var center = JunctionId(1);
        var j1 = JunctionId(2);  // Top-right
        var j2 = JunctionId(3);  // Bottom-right
        var j3 = JunctionId(4);  // Bottom-left
        var j4 = JunctionId(5);  // Top-left

        var b1 = BoundaryId(1);  // center→j1 (N|E)
        var b2 = BoundaryId(2);  // center→j2 (E|S)
        var b3 = BoundaryId(3);  // center→j3 (S|W)
        var b4 = BoundaryId(4);  // center→j4 (W|N)
        var b5 = BoundaryId(5);  // j1→j2 (world|E)
        var b6 = BoundaryId(6);  // j2→j3 (world|S)
        var b7 = BoundaryId(7);  // j3→j4 (world|W)
        var b8 = BoundaryId(8);  // j4→j1 (world|N)

        topology.Plates[north] = new PlateEntity(north, false, null);
        topology.Plates[east] = new PlateEntity(east, false, null);
        topology.Plates[south] = new PlateEntity(south, false, null);
        topology.Plates[west] = new PlateEntity(west, false, null);
        topology.Plates[world] = new PlateEntity(world, false, null);

        // Junctions
        topology.Junctions[center] = new JunctionEntity(center,
            ImmutableArray.Create(b1, b2, b3, b4),
            SurfacePoint.UnitSphere(UnitVector3d.FromComponents(0, 0, 1) ?? UnitVector3d.UnitZ),
            false, null);
        topology.Junctions[j1] = new JunctionEntity(j1,
            ImmutableArray.Create(b1, b5, b8),
            SurfacePoint.UnitSphere(UnitVector3d.FromComponents(1, 1, 1) ?? UnitVector3d.UnitZ),
            false, null);
        topology.Junctions[j2] = new JunctionEntity(j2,
            ImmutableArray.Create(b2, b5, b6),
            SurfacePoint.UnitSphere(UnitVector3d.FromComponents(1, -1, 1) ?? UnitVector3d.UnitZ),
            false, null);
        topology.Junctions[j3] = new JunctionEntity(j3,
            ImmutableArray.Create(b3, b6, b7),
            SurfacePoint.UnitSphere(UnitVector3d.FromComponents(-1, -1, 1) ?? UnitVector3d.UnitZ),
            false, null);
        topology.Junctions[j4] = new JunctionEntity(j4,
            ImmutableArray.Create(b4, b7, b8),
            SurfacePoint.UnitSphere(UnitVector3d.FromComponents(-1, 1, 1) ?? UnitVector3d.UnitZ),
            false, null);

        // Internal boundaries from center to corners
        topology.Boundaries[b1] = new BoundaryEntity(b1, north, east,
            BoundaryType.Divergent,
            new Polyline3([new Point3(0, 0, 0), new Point3(1, 1, 0)]),
            false, null);
        topology.Boundaries[b2] = new BoundaryEntity(b2, east, south,
            BoundaryType.Divergent,
            new Polyline3([new Point3(0, 0, 0), new Point3(1, -1, 0)]),
            false, null);
        topology.Boundaries[b3] = new BoundaryEntity(b3, south, west,
            BoundaryType.Divergent,
            new Polyline3([new Point3(0, 0, 0), new Point3(-1, -1, 0)]),
            false, null);
        topology.Boundaries[b4] = new BoundaryEntity(b4, west, north,
            BoundaryType.Divergent,
            new Polyline3([new Point3(0, 0, 0), new Point3(-1, 1, 0)]),
            false, null);

        // External boundaries forming outer square
        topology.Boundaries[b5] = new BoundaryEntity(b5, world, east,
            BoundaryType.Convergent,
            new Polyline3([new Point3(1, 1, 0), new Point3(1, -1, 0)]),
            false, null);
        topology.Boundaries[b6] = new BoundaryEntity(b6, world, south,
            BoundaryType.Convergent,
            new Polyline3([new Point3(1, -1, 0), new Point3(-1, -1, 0)]),
            false, null);
        topology.Boundaries[b7] = new BoundaryEntity(b7, world, west,
            BoundaryType.Convergent,
            new Polyline3([new Point3(-1, -1, 0), new Point3(-1, 1, 0)]),
            false, null);
        topology.Boundaries[b8] = new BoundaryEntity(b8, world, north,
            BoundaryType.Convergent,
            new Polyline3([new Point3(-1, 1, 0), new Point3(1, 1, 0)]),
            false, null);

        return topology;
    }

    #endregion

    #region Topologies with Gaps

    /// <summary>
    /// Creates a topology with a gap - missing boundary segment.
    /// </summary>
    public static InMemoryTopologyStateView CreateTopologyWithGap()
    {
        var topology = CreateTwoPlateTopology();

        // Remove one boundary to create a gap
        var gapBoundaryId = BoundaryId(2);
        if (topology.Boundaries.ContainsKey(gapBoundaryId))
        {
            var oldBoundary = topology.Boundaries[gapBoundaryId];
            topology.Boundaries[gapBoundaryId] = oldBoundary with { IsRetired = true, RetirementReason = "Gap simulation" };
        }

        return topology;
    }

    /// <summary>
    /// Creates a topology with small gaps between boundaries.
    /// </summary>
    public static InMemoryTopologyStateView CreateTopologyWithSmallGaps(double gapSize = 0.001)
    {
        var topology = new InMemoryTopologyStateView("small-gaps");

        var plateA = PlateId(1);
        var plateB = PlateId(2);

        topology.Plates[plateA] = new PlateEntity(plateA, false, null);
        topology.Plates[plateB] = new PlateEntity(plateB, false, null);

        var j1 = JunctionId(1);
        var j2 = JunctionId(2);
        var j3 = JunctionId(3);
        var j4 = JunctionId(4);

        var b1 = BoundaryId(1);
        var b2 = BoundaryId(2);

        // Create junctions with small gaps between them
        topology.Junctions[j1] = new JunctionEntity(j1, ImmutableArray.Create(b1), SurfacePoint.UnitSphere(UnitVector3d.UnitX), false, null);
        topology.Junctions[j2] = new JunctionEntity(j2, ImmutableArray.Create(b1, b2), SurfacePoint.UnitSphere(UnitVector3d.UnitX), false, null);
        topology.Junctions[j3] = new JunctionEntity(j3, ImmutableArray.Create(b2), SurfacePoint.UnitSphere(UnitVector3d.UnitX), false, null);
        topology.Junctions[j4] = new JunctionEntity(j4, ImmutableArray.Create(b1), SurfacePoint.UnitSphere(UnitVector3d.UnitX), false, null);

        // Boundaries with small gaps between segments
        topology.Boundaries[b1] = new BoundaryEntity(b1, plateA, plateB, BoundaryType.Divergent,
            new Polyline3([
                new Point3(0, -1, 0),
                new Point3(0, -gapSize, 0) // Stops before junction
            ]), false, null);

        topology.Boundaries[b2] = new BoundaryEntity(b2, plateA, plateB, BoundaryType.Divergent,
            new Polyline3([
                new Point3(0, gapSize, 0), // Starts after gap
                new Point3(0, 1, 0)
            ]), false, null);

        return topology;
    }

    #endregion

    #region Topologies with Overlaps

    /// <summary>
    /// Creates a topology with overlapping boundaries.
    /// </summary>
    public static InMemoryTopologyStateView CreateTopologyWithOverlaps()
    {
        var topology = new InMemoryTopologyStateView("overlaps");

        var plateA = PlateId(1);
        var plateB = PlateId(2);
        var plateC = PlateId(3);

        topology.Plates[plateA] = new PlateEntity(plateA, false, null);
        topology.Plates[plateB] = new PlateEntity(plateB, false, null);
        topology.Plates[plateC] = new PlateEntity(plateC, false, null);

        var j1 = JunctionId(1);
        var j2 = JunctionId(2);
        var j3 = JunctionId(3);

        var b1 = BoundaryId(1); // A-B
        var b2 = BoundaryId(2); // B-C (overlapping)
        var b3 = BoundaryId(3); // C-A (overlapping)

        topology.Junctions[j1] = new JunctionEntity(j1, ImmutableArray.Create(b1, b3), SurfacePoint.UnitSphere(UnitVector3d.UnitX), false, null);
        topology.Junctions[j2] = new JunctionEntity(j2, ImmutableArray.Create(b1, b2), SurfacePoint.UnitSphere(UnitVector3d.UnitX), false, null);
        topology.Junctions[j3] = new JunctionEntity(j3, ImmutableArray.Create(b2, b3), SurfacePoint.UnitSphere(UnitVector3d.UnitX), false, null);

        // Overlapping boundaries - same geometry assigned to different plate pairs
        var sharedGeometry = new Polyline3([
            new Point3(-1, 0, 0),
            new Point3(1, 0, 0)
        ]);

        topology.Boundaries[b1] = new BoundaryEntity(b1, plateA, plateB, BoundaryType.Divergent, sharedGeometry, false, null);
        topology.Boundaries[b2] = new BoundaryEntity(b2, plateB, plateC, BoundaryType.Divergent, sharedGeometry, false, null);
        topology.Boundaries[b3] = new BoundaryEntity(b3, plateC, plateA, BoundaryType.Divergent, sharedGeometry, false, null);

        return topology;
    }

    #endregion

    #region Valid Spherical Partitions

    /// <summary>
    /// Creates a valid spherical partition with hemispheres.
    /// </summary>
    public static (InMemoryTopologyStateView Topology, List<PlatePolygon> ExpectedPolygons) CreateHemispherePartition()
    {
        var topology = new InMemoryTopologyStateView("hemispheres");

        var northHemisphere = PlateId(1);
        var southHemisphere = PlateId(2);

        topology.Plates[northHemisphere] = new PlateEntity(northHemisphere, false, null);
        topology.Plates[southHemisphere] = new PlateEntity(southHemisphere, false, null);

        // Create equator ring
        var numPoints = 12;
        var equatorJunctions = new List<JunctionId>();
        var equatorBoundaries = new List<BoundaryId>();

        for (int i = 0; i < numPoints; i++)
        {
            equatorJunctions.Add(JunctionId(i + 1));
            equatorBoundaries.Add(BoundaryId(i + 1));
        }

        // Create junctions on equator
        for (int i = 0; i < numPoints; i++)
        {
            var angle = 2 * Math.PI * i / numPoints;
            var x = Math.Cos(angle);
            var z = Math.Sin(angle);

            var b1 = equatorBoundaries[i];
            var b2 = equatorBoundaries[(i + 1) % numPoints];

            var location = new SurfacePoint(
                new UnitVector3d(x, 0, z),
                1.0);

            topology.Junctions[equatorJunctions[i]] = new JunctionEntity(
                equatorJunctions[i],
                ImmutableArray.Create(b1, b2),
                location,
                false, null);
        }

        // Create equator boundaries
        for (int i = 0; i < numPoints; i++)
        {
            var j1 = equatorJunctions[i];
            var j2 = equatorBoundaries[(i + 1) % numPoints];

            var p1 = topology.Junctions[j1].Location;
            var p2Index = (i + 1) % numPoints;
            var p2 = topology.Junctions[equatorJunctions[p2Index]].Location;

            topology.Boundaries[equatorBoundaries[i]] = new BoundaryEntity(
                equatorBoundaries[i],
                northHemisphere,
                southHemisphere,
                BoundaryType.Divergent,
                new Polyline3([
                    new Point3(p1.Normal.X, p1.Normal.Y, p1.Normal.Z),
                    new Point3(p2.Normal.X, p2.Normal.Y, p2.Normal.Z)
                ]),
                false, null);
        }

        // Build expected polygons
        var expectedPolygons = new List<PlatePolygon>
        {
            new PlatePolygon
            {
                PlateId = northHemisphere,
                OuterBoundary = CreateEquatorPolygon(equatorJunctions.Select(j => topology.Junctions[j].Location).ToList()),
                Holes = ImmutableArray<Polygon>.Empty,
                SphericalArea = 2.0 * Math.PI // Hemisphere area
            },
            new PlatePolygon
            {
                PlateId = southHemisphere,
                OuterBoundary = CreateEquatorPolygon(equatorJunctions.Select(j => topology.Junctions[j].Location).ToList()),
                Holes = ImmutableArray<Polygon>.Empty,
                SphericalArea = 2.0 * Math.PI // Hemisphere area
            }
        };

        return (topology, expectedPolygons);
    }

    /// <summary>
    /// Creates a valid partition with multiple plates covering the sphere.
    /// </summary>
    public static (InMemoryTopologyStateView Topology, List<PlatePolygon> ExpectedPolygons) CreateMultiPlatePartition(int plateCount = 6)
    {
        var topology = new InMemoryTopologyStateView($"{plateCount}-plates");
        var expectedPolygons = new List<PlatePolygon>();

        // Create plates
        for (int i = 0; i < plateCount; i++)
        {
            var plateId = PlateId(i + 1);
            topology.Plates[plateId] = new PlateEntity(plateId, false, null);
        }

        // Create a simplified topology - plates arranged around equator
        // with polar plates at top and bottom

        var polarNorth = PlateId(plateCount + 1);
        var polarSouth = PlateId(plateCount + 2);
        topology.Plates[polarNorth] = new PlateEntity(polarNorth, false, null);
        topology.Plates[polarSouth] = new PlateEntity(polarSouth, false, null);

        // For testing purposes, create simplified expected polygons
        for (int i = 0; i < plateCount; i++)
        {
            var plateId = PlateId(i + 1);
            var ring = CreateSimplifiedPlatePolygon(i, plateCount);
            expectedPolygons.Add(new PlatePolygon
            {
                PlateId = plateId,
                OuterBoundary = ring,
                Holes = ImmutableArray<Polygon>.Empty,
                SphericalArea = 4.0 * Math.PI / plateCount // Equal share of sphere
            });
        }

        return (topology, expectedPolygons);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Creates a topology with a single plate (covering entire sphere).
    /// </summary>
    public static InMemoryTopologyStateView CreateSinglePlateTopology()
    {
        var topology = new InMemoryTopologyStateView("single-plate");

        var plate = PlateId(1);
        topology.Plates[plate] = new PlateEntity(plate, false, null);

        // Single plate - no internal boundaries needed
        return topology;
    }

    /// <summary>
    /// Creates an empty topology.
    /// </summary>
    public static InMemoryTopologyStateView CreateEmptyTopology()
    {
        return new InMemoryTopologyStateView("empty");
    }

    /// <summary>
    /// Creates a topology with a very small plate (sliver).
    /// </summary>
    public static InMemoryTopologyStateView CreateSliverPlateTopology()
    {
        var topology = new InMemoryTopologyStateView("sliver");

        var largePlate = PlateId(1);
        var sliverPlate = PlateId(2);

        topology.Plates[largePlate] = new PlateEntity(largePlate, false, null);
        topology.Plates[sliverPlate] = new PlateEntity(sliverPlate, false, null);

        var j1 = JunctionId(1);
        var j2 = JunctionId(2);

        var b1 = BoundaryId(1);

        topology.Junctions[j1] = new JunctionEntity(j1, ImmutableArray.Create(b1), SurfacePoint.UnitSphere(UnitVector3d.UnitX), false, null);
        topology.Junctions[j2] = new JunctionEntity(j2, ImmutableArray.Create(b1), SurfacePoint.UnitSphere(UnitVector3d.UnitX), false, null);

        // Very small boundary creates sliver plate
        topology.Boundaries[b1] = new BoundaryEntity(b1, largePlate, sliverPlate, BoundaryType.Divergent,
            new Polyline3([
                new Point3(0, 0, 0),
                new Point3(1e-10, 0, 0)
            ]), false, null);

        return topology;
    }

    #endregion

    #region Partition Requests

    public static PartitionRequest CreatePartitionRequest(
        int tick = 100,
        TolerancePolicy? policy = null,
        PartitionOptions? options = null)
    {
        return new PartitionRequest
        {
            Tick = new CanonicalTick(tick),
            TolerancePolicy = policy ?? new TolerancePolicy.StrictPolicy(),
            Options = options
        };
    }

    #endregion

    #region Helpers

    private static Polygon CreateEquatorPolygon(List<SurfacePoint> points)
    {
        var point3s = points.Select(p => new Point3(p.Normal.X, p.Normal.Y, p.Normal.Z)).ToList();
        point3s.Add(point3s[0]); // Close the ring
        return new Polygon(point3s);
    }

    private static Polygon CreateSimplifiedPlatePolygon(int index, int totalPlates)
    {
        var angle1 = 2 * Math.PI * index / totalPlates;
        var angle2 = 2 * Math.PI * (index + 1) / totalPlates;

        var r = 0.9; // Near-unit radius

        var points = new List<Point3>
        {
            new(r * Math.Cos(angle1), 0.5, r * Math.Sin(angle1)),
            new(r * Math.Cos(angle2), 0.5, r * Math.Sin(angle2)),
            new(r * Math.Cos(angle2), -0.5, r * Math.Sin(angle2)),
            new(r * Math.Cos(angle1), -0.5, r * Math.Sin(angle1)),
            new(r * Math.Cos(angle1), 0.5, r * Math.Sin(angle1)) // Close
        };

        return new Polygon(points);
    }

    #endregion
}
