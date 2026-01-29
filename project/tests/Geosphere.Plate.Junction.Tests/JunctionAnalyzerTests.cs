using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Junction.Contracts.Products;
using FantaSim.Geosphere.Plate.Junction.Contracts.Solvers;
using FantaSim.Geosphere.Plate.Junction.Solver;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using NSubstitute;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Junction.Tests;

/// <summary>
/// Tests for JunctionAnalyzer - verifies junction enumeration, incident ordering, and classification.
/// RFC-V2-0042 implementation tests.
/// </summary>
public class JunctionAnalyzerTests
{
    #region Test Helpers

    private static IPlateTopologyStateView CreateMockTopology(
        Dictionary<JunctionId, Topology.Contracts.Entities.Junction> junctions,
        Dictionary<BoundaryId, Boundary> boundaries,
        Dictionary<PlateId, Topology.Contracts.Entities.Plate>? plates = null)
    {
        var mock = Substitute.For<IPlateTopologyStateView>();
        mock.Junctions.Returns(junctions);
        mock.Boundaries.Returns(boundaries);
        mock.Plates.Returns(plates ?? new Dictionary<PlateId, Topology.Contracts.Entities.Plate>());
        mock.LastEventSequence.Returns(1);
        return mock;
    }

    private static JunctionId MakeJunction(int seed) =>
        new(new Guid(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

    private static BoundaryId MakeBoundary(int seed) =>
        new(new Guid(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

    private static PlateId MakePlate(int seed) =>
        new(new Guid(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

    private static Topology.Contracts.Entities.Junction CreateJunction(
        JunctionId id, double x, double y, params BoundaryId[] boundaryIds) =>
        new(id, boundaryIds, new Point2(x, y), IsRetired: false, RetirementReason: null);

    private static Boundary CreateBoundary(
        BoundaryId id,
        PlateId leftPlate,
        PlateId rightPlate,
        BoundaryType type,
        Point3 start,
        Point3 end) =>
        new(id, leftPlate, rightPlate, type, new Polyline3([start, end]), IsRetired: false, RetirementReason: null);

    private static JunctionAnalyzer CreateAnalyzer() => new();

    private static readonly CanonicalTick TestTick = new(100);

    #endregion

    #region Basic Junction Enumeration

    /// <summary>
    /// Verify that a triple junction has exactly 3 incidents.
    /// RFC-V2-0042 §15.1: TripleJunction_HasThreeIncidents
    /// </summary>
    [Fact]
    public void BuildJunctionSet_TripleJunction_HasThreeIncidents()
    {
        // Arrange: A triple junction at origin with 3 boundaries
        var j1 = MakeJunction(1);
        var b1 = MakeBoundary(1);
        var b2 = MakeBoundary(2);
        var b3 = MakeBoundary(3);

        var pA = MakePlate(1);
        var pB = MakePlate(2);
        var pC = MakePlate(3);

        var junctions = new Dictionary<JunctionId, Topology.Contracts.Entities.Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1, b2, b3)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, pA, pB, BoundaryType.Divergent,
                new Point3(0, 0, 0), new Point3(1, 0, 0)),
            [b2] = CreateBoundary(b2, pB, pC, BoundaryType.Divergent,
                new Point3(0, 0, 0), new Point3(-0.5, 0.866, 0)),
            [b3] = CreateBoundary(b3, pC, pA, BoundaryType.Divergent,
                new Point3(0, 0, 0), new Point3(-0.5, -0.866, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var analyzer = CreateAnalyzer();

        // Act
        var result = analyzer.BuildJunctionSet(TestTick, topology);

        // Assert
        Assert.Single(result.Junctions);
        var junction = result.Junctions[0];
        Assert.Equal(3, junction.Incidents.Length);
        Assert.True(junction.IsTriple);
    }

    /// <summary>
    /// Verify incident ordering is deterministic (same inputs → same order).
    /// RFC-V2-0042 §15.1: IncidentOrdering_IsDeterministic
    /// </summary>
    [Fact]
    public void BuildJunctionSet_SameInputs_ProducesSameIncidentOrder()
    {
        // Arrange
        var j1 = MakeJunction(1);
        var b1 = MakeBoundary(1);
        var b2 = MakeBoundary(2);
        var b3 = MakeBoundary(3);

        var pA = MakePlate(1);
        var pB = MakePlate(2);
        var pC = MakePlate(3);

        var junctions = new Dictionary<JunctionId, Topology.Contracts.Entities.Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1, b2, b3)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, pA, pB, BoundaryType.Divergent,
                new Point3(0, 0, 0), new Point3(1, 0, 0)),
            [b2] = CreateBoundary(b2, pB, pC, BoundaryType.Transform,
                new Point3(0, 0, 0), new Point3(0, 1, 0)),
            [b3] = CreateBoundary(b3, pC, pA, BoundaryType.Convergent,
                new Point3(0, 0, 0), new Point3(-1, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var analyzer = CreateAnalyzer();

        // Act: Run twice
        var result1 = analyzer.BuildJunctionSet(TestTick, topology);
        var result2 = analyzer.BuildJunctionSet(TestTick, topology);

        // Assert: Same ordering
        var incidents1 = result1.Junctions[0].Incidents;
        var incidents2 = result2.Junctions[0].Incidents;

        Assert.Equal(incidents1.Length, incidents2.Length);
        for (int i = 0; i < incidents1.Length; i++)
        {
            Assert.Equal(incidents1[i].BoundaryId, incidents2[i].BoundaryId);
            Assert.Equal(incidents1[i].Angle, incidents2[i].Angle, precision: 10);
        }
    }

    #endregion

    #region Triple Junction Classification

    /// <summary>
    /// Verify RRR classification for three divergent boundaries.
    /// RFC-V2-0042 §15.1: RRR_Classification
    /// </summary>
    [Fact]
    public void BuildJunctionSet_ThreeDivergentBoundaries_ClassifiedAsRRR()
    {
        // Arrange
        var j1 = MakeJunction(1);
        var b1 = MakeBoundary(1);
        var b2 = MakeBoundary(2);
        var b3 = MakeBoundary(3);

        var pA = MakePlate(1);
        var pB = MakePlate(2);
        var pC = MakePlate(3);

        var junctions = new Dictionary<JunctionId, Topology.Contracts.Entities.Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1, b2, b3)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, pA, pB, BoundaryType.Divergent,
                new Point3(0, 0, 0), new Point3(1, 0, 0)),
            [b2] = CreateBoundary(b2, pB, pC, BoundaryType.Divergent,
                new Point3(0, 0, 0), new Point3(0, 1, 0)),
            [b3] = CreateBoundary(b3, pC, pA, BoundaryType.Divergent,
                new Point3(0, 0, 0), new Point3(-1, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var analyzer = CreateAnalyzer();

        // Act
        var result = analyzer.BuildJunctionSet(TestTick, topology);

        // Assert
        Assert.Single(result.Junctions);
        Assert.Equal(JunctionClassification.RRR, result.Junctions[0].Classification);
    }

    /// <summary>
    /// Verify RTT classification for one divergent and two convergent boundaries.
    /// RFC-V2-0042 §15.1: RTT_Classification
    /// </summary>
    [Fact]
    public void BuildJunctionSet_OneDivergentTwoConvergent_ClassifiedAsRTT()
    {
        // Arrange
        var j1 = MakeJunction(1);
        var b1 = MakeBoundary(1);
        var b2 = MakeBoundary(2);
        var b3 = MakeBoundary(3);

        var pA = MakePlate(1);
        var pB = MakePlate(2);
        var pC = MakePlate(3);

        var junctions = new Dictionary<JunctionId, Topology.Contracts.Entities.Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1, b2, b3)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, pA, pB, BoundaryType.Divergent,
                new Point3(0, 0, 0), new Point3(1, 0, 0)),
            [b2] = CreateBoundary(b2, pB, pC, BoundaryType.Convergent,
                new Point3(0, 0, 0), new Point3(0, 1, 0)),
            [b3] = CreateBoundary(b3, pC, pA, BoundaryType.Convergent,
                new Point3(0, 0, 0), new Point3(-1, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var analyzer = CreateAnalyzer();

        // Act
        var result = analyzer.BuildJunctionSet(TestTick, topology);

        // Assert
        Assert.Single(result.Junctions);
        Assert.Equal(JunctionClassification.RTT, result.Junctions[0].Classification);
    }

    /// <summary>
    /// Verify RFT classification for one each of Ridge, Fault, Trench.
    /// </summary>
    [Fact]
    public void BuildJunctionSet_OneDivergentOneTransformOneConvergent_ClassifiedAsRFT()
    {
        // Arrange
        var j1 = MakeJunction(1);
        var b1 = MakeBoundary(1);
        var b2 = MakeBoundary(2);
        var b3 = MakeBoundary(3);

        var pA = MakePlate(1);
        var pB = MakePlate(2);
        var pC = MakePlate(3);

        var junctions = new Dictionary<JunctionId, Topology.Contracts.Entities.Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1, b2, b3)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, pA, pB, BoundaryType.Divergent,
                new Point3(0, 0, 0), new Point3(1, 0, 0)),
            [b2] = CreateBoundary(b2, pB, pC, BoundaryType.Transform,
                new Point3(0, 0, 0), new Point3(0, 1, 0)),
            [b3] = CreateBoundary(b3, pC, pA, BoundaryType.Convergent,
                new Point3(0, 0, 0), new Point3(-1, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var analyzer = CreateAnalyzer();

        // Act
        var result = analyzer.BuildJunctionSet(TestTick, topology);

        // Assert
        Assert.Single(result.Junctions);
        Assert.Equal(JunctionClassification.RFT, result.Junctions[0].Classification);
    }

    #endregion

    #region Incident Plates

    /// <summary>
    /// Verify that incident plates are correctly identified and sorted.
    /// </summary>
    [Fact]
    public void BuildJunctionSet_TripleJunction_HasThreeSortedPlates()
    {
        // Arrange
        var j1 = MakeJunction(1);
        var b1 = MakeBoundary(1);
        var b2 = MakeBoundary(2);
        var b3 = MakeBoundary(3);

        var pA = MakePlate(1);
        var pB = MakePlate(2);
        var pC = MakePlate(3);

        var junctions = new Dictionary<JunctionId, Topology.Contracts.Entities.Junction>
        {
            [j1] = CreateJunction(j1, 0, 0, b1, b2, b3)
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>
        {
            [b1] = CreateBoundary(b1, pA, pB, BoundaryType.Divergent,
                new Point3(0, 0, 0), new Point3(1, 0, 0)),
            [b2] = CreateBoundary(b2, pB, pC, BoundaryType.Divergent,
                new Point3(0, 0, 0), new Point3(0, 1, 0)),
            [b3] = CreateBoundary(b3, pC, pA, BoundaryType.Divergent,
                new Point3(0, 0, 0), new Point3(-1, 0, 0))
        };

        var topology = CreateMockTopology(junctions, boundaries);
        var analyzer = CreateAnalyzer();

        // Act
        var result = analyzer.BuildJunctionSet(TestTick, topology);

        // Assert
        var junction = result.Junctions[0];
        Assert.Equal(3, junction.IncidentPlates.Length);

        // Plates should be sorted by PlateId.Value
        var sortedPlates = junction.IncidentPlates.ToList();
        var expectedOrder = new[] { pA, pB, pC }.OrderBy(p => p.Value).ToList();
        Assert.Equal(expectedOrder, sortedPlates);
    }

    #endregion

    #region Empty and Edge Cases

    /// <summary>
    /// Verify empty topology returns empty junction set.
    /// </summary>
    [Fact]
    public void BuildJunctionSet_EmptyTopology_ReturnsEmptySet()
    {
        // Arrange
        var junctions = new Dictionary<JunctionId, Topology.Contracts.Entities.Junction>();
        var boundaries = new Dictionary<BoundaryId, Boundary>();

        var topology = CreateMockTopology(junctions, boundaries);
        var analyzer = CreateAnalyzer();

        // Act
        var result = analyzer.BuildJunctionSet(TestTick, topology);

        // Assert
        Assert.Empty(result.Junctions);
        Assert.Equal(TestTick, result.Tick);
    }

    /// <summary>
    /// Verify retired junctions are excluded.
    /// </summary>
    [Fact]
    public void BuildJunctionSet_RetiredJunction_IsExcluded()
    {
        // Arrange
        var j1 = MakeJunction(1);
        var b1 = MakeBoundary(1);

        var junctions = new Dictionary<JunctionId, Topology.Contracts.Entities.Junction>
        {
            [j1] = new Topology.Contracts.Entities.Junction(
                j1, [b1], new Point2(0, 0), IsRetired: true, RetirementReason: "Test")
        };

        var boundaries = new Dictionary<BoundaryId, Boundary>();
        var topology = CreateMockTopology(junctions, boundaries);
        var analyzer = CreateAnalyzer();

        // Act
        var result = analyzer.BuildJunctionSet(TestTick, topology);

        // Assert
        Assert.Empty(result.Junctions);
    }

    #endregion

    #region JunctionSet Helpers

    /// <summary>
    /// Verify GetJunction lookup works.
    /// </summary>
    [Fact]
    public void JunctionSet_GetJunction_FindsCorrectJunction()
    {
        // Arrange
        var j1 = MakeJunction(1);
        var jInfo = new JunctionInfo(
            j1,
            new Point3(1, 2, 0),
            ImmutableArray<JunctionIncident>.Empty,
            ImmutableArray<PlateId>.Empty,
            null);

        var set = new JunctionSet(TestTick, [jInfo]);

        // Act
        var found = set.GetJunction(j1);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(j1, found.Value.JunctionId);
    }

    /// <summary>
    /// Verify GetJunctionsForPlate filters correctly.
    /// </summary>
    [Fact]
    public void JunctionSet_GetJunctionsForPlate_FiltersCorrectly()
    {
        // Arrange
        var j1 = MakeJunction(1);
        var j2 = MakeJunction(2);
        var pA = MakePlate(1);
        var pB = MakePlate(2);

        var jInfo1 = new JunctionInfo(
            j1,
            new Point3(1, 0, 0),
            ImmutableArray<JunctionIncident>.Empty,
            [pA, pB],
            null);

        var jInfo2 = new JunctionInfo(
            j2,
            new Point3(2, 0, 0),
            ImmutableArray<JunctionIncident>.Empty,
            [pB],
            null);

        var set = new JunctionSet(TestTick, [jInfo1, jInfo2]);

        // Act
        var forPlateA = set.GetJunctionsForPlate(pA).ToList();
        var forPlateB = set.GetJunctionsForPlate(pB).ToList();

        // Assert
        Assert.Single(forPlateA);
        Assert.Equal(j1, forPlateA[0].JunctionId);

        Assert.Equal(2, forPlateB.Count);
    }

    #endregion
}
