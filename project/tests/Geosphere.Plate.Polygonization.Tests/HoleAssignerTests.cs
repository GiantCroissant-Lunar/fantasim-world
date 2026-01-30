using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Polygonization.Solver;
using FluentAssertions;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Polygonization.Tests;

/// <summary>
/// Tests for <see cref="HoleAssigner"/> focusing on RFC-V2-0041 §9.4 compliance.
///
/// Key invariants tested:
/// 1. Rings are classified by signed area (CCW = outer, CW = hole)
/// 2. Each hole is assigned to the innermost containing outer
/// 3. Multiple outers + multiple holes produce deterministic assignment
/// </summary>
public sealed class HoleAssignerTests
{
    #region 1️⃣ Basic Classification

    [Fact]
    public void AssignHoles_SingleOuter_NoHoles_ReturnsSingleGroup()
    {
        // CCW square (positive area = outer)
        var outer = CreateCCWSquare(0, 0, 2);

        var result = HoleAssigner.AssignHoles([outer]);

        result.Should().HaveCount(1);
        result[0].OuterRing.Should().Be(outer);
        result[0].Holes.Should().BeEmpty();
    }

    [Fact]
    public void AssignHoles_SingleOuter_SingleHole_AssignsCorrectly()
    {
        // Large CCW outer
        var outer = CreateCCWSquare(0, 0, 4);
        // Small CW hole inside
        var hole = CreateCWSquare(1, 1, 1);

        var result = HoleAssigner.AssignHoles([outer, hole]);

        result.Should().HaveCount(1);
        result[0].OuterRing.Should().Be(outer);
        result[0].Holes.Should().HaveCount(1);
        result[0].Holes[0].Should().Be(hole);
    }

    #endregion

    #region 2️⃣ Nested Holes & Disjoint Outers (RFC-V2-0041 "evil determinism")

    /// <summary>
    /// RFC-V2-0041 §9.4: With two outers + multiple holes, ensure each hole
    /// is assigned to the innermost containing outer deterministically.
    ///
    /// Layout:
    /// ┌────────────────────────────────────────┐
    /// │ Outer A (large)                        │
    /// │   ┌────────────┐    ┌────────────┐     │
    /// │   │ Outer B    │    │ Hole 1     │     │
    /// │   │ (smaller)  │    │ (inside A) │     │
    /// │   │  ┌──────┐  │    └────────────┘     │
    /// │   │  │Hole 2│  │                       │
    /// │   │  │(in B)│  │                       │
    /// │   │  └──────┘  │                       │
    /// │   └────────────┘                       │
    /// └────────────────────────────────────────┘
    ///
    /// Expected assignment:
    /// - Hole 1 → Outer A (only A contains it)
    /// - Hole 2 → Outer B (B is innermost containing outer)
    /// </summary>
    [Fact]
    public void AssignHoles_TwoOutersWithNestedHoles_AssignsToInnermostOuter()
    {
        // Outer A: large square centered at (5,5), size 10
        var outerA = CreateCCWSquare(0, 0, 10);

        // Outer B: smaller square centered at (2,2), size 4 (inside A)
        var outerB = CreateCCWSquare(1, 1, 4);

        // Hole 1: inside A but outside B
        var hole1 = CreateCWSquare(7, 7, 1);

        // Hole 2: inside B (and therefore also inside A)
        var hole2 = CreateCWSquare(2, 2, 1);

        // Randomize input order to test determinism
        var result1 = HoleAssigner.AssignHoles([outerA, outerB, hole1, hole2]);
        var result2 = HoleAssigner.AssignHoles([hole2, outerB, hole1, outerA]);
        var result3 = HoleAssigner.AssignHoles([hole1, hole2, outerA, outerB]);

        // All results should be identical
        AssertGroupsEqual(result1, result2);
        AssertGroupsEqual(result1, result3);

        // Verify correct assignment
        result1.Should().HaveCount(2);

        // Find which group is A and which is B by area
        var groupA = result1.First(g => RingCanonicalizer.ComputeSignedArea(g.OuterRing) > 50);
        var groupB = result1.First(g => RingCanonicalizer.ComputeSignedArea(g.OuterRing) < 50);

        // Hole 1 should be in A (outside B)
        groupA.Holes.Should().Contain(h => AreSameRing(h, hole1));

        // Hole 2 should be in B (innermost containing)
        groupB.Holes.Should().Contain(h => AreSameRing(h, hole2));
    }

    /// <summary>
    /// Two completely disjoint outers, each with its own hole.
    /// Verifies that holes are correctly matched to their containing outer.
    /// </summary>
    [Fact]
    public void AssignHoles_DisjointOutersWithSeparateHoles_AssignsCorrectly()
    {
        // Outer A at left (0,0)
        var outerA = CreateCCWSquare(0, 0, 4);
        var holeA = CreateCWSquare(1, 1, 1);

        // Outer B at right (10,0) - completely disjoint
        var outerB = CreateCCWSquare(10, 0, 4);
        var holeB = CreateCWSquare(11, 1, 1);

        var result = HoleAssigner.AssignHoles([outerA, outerB, holeA, holeB]);

        result.Should().HaveCount(2);

        // Verify each outer has exactly its own hole
        var sortedGroups = result.OrderBy(g => g.OuterRing[0].X).ToList();

        // Group at X=0 should have holeA
        sortedGroups[0].Holes.Should().HaveCount(1);
        sortedGroups[0].Holes[0][0].X.Should().BeApproximately(1, 0.1);

        // Group at X=10 should have holeB
        sortedGroups[1].Holes.Should().HaveCount(1);
        sortedGroups[1].Holes[0][0].X.Should().BeApproximately(11, 0.1);
    }

    /// <summary>
    /// Multiple levels of nesting to stress-test innermost selection.
    ///
    /// Layout: A contains B, B contains C (all outers), with hole in C.
    /// </summary>
    [Fact]
    public void AssignHoles_DeeplyNestedOuters_AssignsToInnermostContainer()
    {
        // Outer A (largest)
        var outerA = CreateCCWSquare(0, 0, 20);

        // Outer B (medium, inside A)
        var outerB = CreateCCWSquare(2, 2, 10);

        // Outer C (smallest, inside B)
        var outerC = CreateCCWSquare(4, 4, 4);

        // Hole inside C
        var hole = CreateCWSquare(5, 5, 1);

        var result = HoleAssigner.AssignHoles([outerA, outerB, outerC, hole]);

        result.Should().HaveCount(3);

        // Find the smallest outer (C) - it should have the hole
        var smallest = result.OrderBy(g => Math.Abs(RingCanonicalizer.ComputeSignedArea(g.OuterRing))).First();
        smallest.Holes.Should().HaveCount(1);

        // Larger outers should have no holes
        var others = result.Where(g => g != smallest).ToList();
        others.Should().AllSatisfy(g => g.Holes.Should().BeEmpty());
    }

    #endregion

    #region 3️⃣ Determinism Under Input Permutation

    /// <summary>
    /// Shuffling input order should not change the output structure.
    /// This test uses a fixed seed random shuffle to ensure reproducibility.
    /// </summary>
    [Fact]
    public void AssignHoles_InputOrderVariation_ProducesDeterministicOutput()
    {
        var outer1 = CreateCCWSquare(0, 0, 6);
        var outer2 = CreateCCWSquare(10, 0, 6);
        var hole1 = CreateCWSquare(1, 1, 2);
        var hole2 = CreateCWSquare(11, 1, 2);

        var inputs = new[] { outer1, outer2, hole1, hole2 };

        // Try all 24 permutations of 4 elements
        var permutations = GetPermutations(inputs).ToList();
        var results = permutations.Select(p => HoleAssigner.AssignHoles(p.ToArray())).ToList();

        // All results should have the same number of groups
        var baseline = results[0];
        foreach (var r in results.Skip(1))
        {
            r.Count.Should().Be(baseline.Count, "All permutations should produce same number of groups");
        }

        // All results should have same total number of holes assigned
        var baselineHoleCount = baseline.Sum(g => g.Holes.Length);
        foreach (var r in results.Skip(1))
        {
            r.Sum(g => g.Holes.Length).Should().Be(baselineHoleCount,
                "All permutations should assign same number of holes");
        }
    }

    #endregion

    #region 4️⃣ Edge Cases

    [Fact]
    public void AssignHoles_EmptyInput_ReturnsEmpty()
    {
        var result = HoleAssigner.AssignHoles([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void AssignHoles_OnlyHoles_ReturnsEmpty()
    {
        // All CW (holes) - no outer to assign them to
        var hole1 = CreateCWSquare(0, 0, 2);
        var hole2 = CreateCWSquare(5, 5, 2);

        var result = HoleAssigner.AssignHoles([hole1, hole2]);

        // No valid outers = no groups
        result.Should().BeEmpty();
    }

    [Fact]
    public void AssignHoles_OrphanedHole_NotContainedByAnyOuter()
    {
        var outer = CreateCCWSquare(0, 0, 4);
        var orphanHole = CreateCWSquare(100, 100, 1); // Far outside outer

        var result = HoleAssigner.AssignHoles([outer, orphanHole]);

        result.Should().HaveCount(1);
        result[0].OuterRing.Should().Be(outer);
        // Orphan hole is not contained, so outer should have no holes
        result[0].Holes.Should().BeEmpty();
    }

    [Fact]
    public void AssignHoles_HoleFarOutsideBoundingBox_NotAssigned()
    {
        // Outer: square from (0,0) to (10,10)
        var outer = CreateCCWSquare(0, 0, 10);
        // Hole: very far away at (1000, 1000)
        var farHole = CreateCWSquare(1000, 1000, 1);

        var result = HoleAssigner.AssignHoles([outer, farHole]);

        result.Should().HaveCount(1);
        result[0].OuterRing.Should().Be(outer);
        result[0].Holes.Should().BeEmpty("hole centroid is far outside bounding box");
    }

    [Fact]
    public void AssignHoles_HoleWithHorizontalEdgeAlignment_CorrectlyContained()
    {
        // Create an outer with a horizontal edge at y=5
        // Outer: (0,0) -> (10,0) -> (10,10) -> (0,10) -> (0,0)
        var outer = CreateCCWSquare(0, 0, 10);
        // Hole with centroid on the same y-coordinate as a horizontal edge
        // Hole: small square at (4,4) to (6,6), centroid at (5,5)
        var holeAligned = CreateCWSquare(4, 4, 2);

        var result = HoleAssigner.AssignHoles([outer, holeAligned]);

        result.Should().HaveCount(1);
        result[0].OuterRing.Should().Be(outer);
        result[0].Holes.Should().HaveCount(1, "hole centroid at (5,5) is inside outer");
    }

    [Fact]
    public void AssignHoles_HoleJustOutsideEdge_NotAssigned()
    {
        // Outer: square from (0,0) to (4,4)
        var outer = CreateCCWSquare(0, 0, 4);
        // Hole: just outside the right edge at x=5
        var justOutside = CreateCWSquare(5, 1, 1); // Centroid at (5.5, 1.5)

        var result = HoleAssigner.AssignHoles([outer, justOutside]);

        result.Should().HaveCount(1);
        result[0].OuterRing.Should().Be(outer);
        result[0].Holes.Should().BeEmpty("hole centroid is outside outer boundary");
    }

    #endregion

    #region 5️⃣ AssignHolesWithOrphans (Explicit Orphan Tracking)

    [Fact]
    public void AssignHolesWithOrphans_OrphanedHole_ReturnedInOrphansList()
    {
        var outer = CreateCCWSquare(0, 0, 4);
        var orphanHole = CreateCWSquare(100, 100, 1); // Far outside outer

        var result = HoleAssigner.AssignHolesWithOrphans([outer, orphanHole]);

        result.Groups.Should().HaveCount(1);
        result.Groups[0].OuterRing.Should().Be(outer);
        result.Groups[0].Holes.Should().BeEmpty();

        // Orphan explicitly returned
        result.OrphanHoles.Should().HaveCount(1);
        result.OrphanHoles[0].Should().Be(orphanHole);
    }

    [Fact]
    public void AssignHolesWithOrphans_MultipleOrphans_AllReturned()
    {
        var outer = CreateCCWSquare(0, 0, 4);
        var orphan1 = CreateCWSquare(100, 100, 1);
        var orphan2 = CreateCWSquare(-50, -50, 1);
        var contained = CreateCWSquare(1, 1, 1);

        var result = HoleAssigner.AssignHolesWithOrphans([outer, orphan1, orphan2, contained]);

        result.Groups.Should().HaveCount(1);
        result.Groups[0].Holes.Should().HaveCount(1);
        result.OrphanHoles.Should().HaveCount(2, "both orphans should be returned");
    }

    [Fact]
    public void AssignHolesWithOrphans_OnlyHoles_AllReturnedAsOrphans()
    {
        // All CW (holes) - no outer to assign them to
        var hole1 = CreateCWSquare(0, 0, 2);
        var hole2 = CreateCWSquare(5, 5, 2);

        var result = HoleAssigner.AssignHolesWithOrphans([hole1, hole2]);

        result.Groups.Should().BeEmpty();
        result.OrphanHoles.Should().HaveCount(2, "all holes are orphans when no outer exists");
    }

    [Fact]
    public void AssignHolesWithOrphans_NoOrphans_OrphanListEmpty()
    {
        var outer = CreateCCWSquare(0, 0, 10);
        var hole = CreateCWSquare(2, 2, 2);

        var result = HoleAssigner.AssignHolesWithOrphans([outer, hole]);

        result.Groups.Should().HaveCount(1);
        result.Groups[0].Holes.Should().HaveCount(1);
        result.OrphanHoles.Should().BeEmpty("hole is properly contained");
    }

    /// <summary>
    /// Policy test: centroid exactly on boundary edge is treated as OUTSIDE.
    /// This is the standard ray-casting behavior (strict inequality on one side).
    /// </summary>
    [Fact]
    public void AssignHolesWithOrphans_CentroidExactlyOnBoundary_TreatedAsOutside()
    {
        // Outer: square from (0,0) to (4,4)
        var outer = CreateCCWSquare(0, 0, 4);
        // Hole with centroid exactly on right edge at x=4
        // Small square at (3.5, 1.5) to (4.5, 2.5), centroid at (4, 2)
        var edgeHole = new Polyline3([
            new Point3(3.5, 1.5, 0),
            new Point3(3.5, 2.5, 0),
            new Point3(4.5, 2.5, 0),
            new Point3(4.5, 1.5, 0),
            new Point3(3.5, 1.5, 0)
        ]);

        var result = HoleAssigner.AssignHolesWithOrphans([outer, edgeHole]);

        // Policy: point on boundary is outside (standard ray-casting behavior)
        result.Groups[0].Holes.Should().BeEmpty("centroid on boundary is treated as outside");
        result.OrphanHoles.Should().HaveCount(1);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates a CCW (counter-clockwise) square (positive signed area = outer).
    /// </summary>
    private static Polyline3 CreateCCWSquare(double x, double y, double size)
    {
        // CCW: bottom-left → bottom-right → top-right → top-left → close
        return new Polyline3([
            new Point3(x, y, 0),
            new Point3(x + size, y, 0),
            new Point3(x + size, y + size, 0),
            new Point3(x, y + size, 0),
            new Point3(x, y, 0)
        ]);
    }

    /// <summary>
    /// Creates a CW (clockwise) square (negative signed area = hole).
    /// </summary>
    private static Polyline3 CreateCWSquare(double x, double y, double size)
    {
        // CW: bottom-left → top-left → top-right → bottom-right → close
        return new Polyline3([
            new Point3(x, y, 0),
            new Point3(x, y + size, 0),
            new Point3(x + size, y + size, 0),
            new Point3(x + size, y, 0),
            new Point3(x, y, 0)
        ]);
    }

    private static void AssertGroupsEqual(
        IReadOnlyList<HoleAssigner.PlateRingGroup> a,
        IReadOnlyList<HoleAssigner.PlateRingGroup> b)
    {
        a.Count.Should().Be(b.Count);

        // Sort both by outer ring area for comparison
        var sortedA = a.OrderByDescending(g => Math.Abs(RingCanonicalizer.ComputeSignedArea(g.OuterRing))).ToList();
        var sortedB = b.OrderByDescending(g => Math.Abs(RingCanonicalizer.ComputeSignedArea(g.OuterRing))).ToList();

        for (var i = 0; i < sortedA.Count; i++)
        {
            AreSameRing(sortedA[i].OuterRing, sortedB[i].OuterRing).Should().BeTrue();
            sortedA[i].Holes.Length.Should().Be(sortedB[i].Holes.Length);
        }
    }

    private static bool AreSameRing(Polyline3 a, Polyline3 b)
    {
        if (a.Count != b.Count) return false;

        // Compare centroids (rings may be rotated differently)
        var centroidA = ComputeCentroid(a);
        var centroidB = ComputeCentroid(b);

        return Math.Abs(centroidA.X - centroidB.X) < 0.1 &&
               Math.Abs(centroidA.Y - centroidB.Y) < 0.1;
    }

    private static Point3 ComputeCentroid(Polyline3 ring)
    {
        var count = ring.Count - 1; // Exclude closing point
        if (count <= 0) return new Point3(0, 0, 0);

        double sumX = 0, sumY = 0;
        for (var i = 0; i < count; i++)
        {
            sumX += ring[i].X;
            sumY += ring[i].Y;
        }
        return new Point3(sumX / count, sumY / count, 0);
    }

    private static IEnumerable<IEnumerable<T>> GetPermutations<T>(T[] items)
    {
        if (items.Length <= 1)
        {
            yield return items;
            yield break;
        }

        foreach (var perm in GetPermutationsRecursive(items, 0))
        {
            yield return perm;
        }
    }

    private static IEnumerable<T[]> GetPermutationsRecursive<T>(T[] items, int start)
    {
        if (start >= items.Length - 1)
        {
            yield return (T[])items.Clone();
            yield break;
        }

        for (var i = start; i < items.Length; i++)
        {
            (items[start], items[i]) = (items[i], items[start]);
            foreach (var perm in GetPermutationsRecursive(items, start + 1))
            {
                yield return perm;
            }
            (items[start], items[i]) = (items[i], items[start]);
        }
    }

    #endregion
}
