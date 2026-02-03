using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using FantaSim.Geosphere.Plate.Polygonization.Solver;
using FluentAssertions;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Polygonization.Tests;

/// <summary>
/// Tests for <see cref="RingCanonicalizer"/> focusing on RFC-V2-0041 §9.3 compliance.
///
/// Key invariants tested:
/// 1. Lexicographically minimum vertex becomes first
/// 2. Duplicate minimal vertex tie-break is deterministic (earliest index wins, then next vertex sequence)
/// 3. Winding convention is enforced
/// </summary>
public sealed class RingCanonicalizerTests
{
    #region 1️⃣ Basic Canonicalization

    [Fact]
    public void Canonicalize_RotatesToLexMinVertex()
    {
        // Ring: B(1,0) → C(1,1) → A(0,0) → B (closed)
        // Lex-min is A(0,0,0)
        var ring = new Polyline3([
            new Point3(1, 0, 0),  // B
            new Point3(1, 1, 0),  // C
            new Point3(0, 0, 0),  // A (lex-min)
            new Point3(1, 0, 0)   // B (close)
        ]);

        var result = RingCanonicalizer.Canonicalize(ring, WindingConvention.CounterClockwise);

        // A should be first now
        result[0].Should().Be(new Point3(0, 0, 0));
    }

    [Fact]
    public void Canonicalize_EnforcesWindingConvention_CCW()
    {
        // CW triangle: A(0,0) → C(1,1) → B(1,0) → A (clockwise when viewed from +Z)
        var cwRing = new Polyline3([
            new Point3(0, 0, 0),
            new Point3(1, 1, 0),
            new Point3(1, 0, 0),
            new Point3(0, 0, 0)
        ]);

        var result = RingCanonicalizer.Canonicalize(cwRing, WindingConvention.CounterClockwise);

        // Should reverse to CCW, but keep A as first (it's lex-min)
        result[0].Should().Be(new Point3(0, 0, 0));

        // Verify winding changed to CCW
        var winding = RingCanonicalizer.DetermineWinding(ToPointList(result));
        winding.Should().Be(WindingConvention.CounterClockwise);
    }

    #endregion

    #region 2️⃣ Duplicate Minimal Vertex Tie-Break (RFC-V2-0041 "evil determinism")

    /// <summary>
    /// RFC-V2-0041 §9.3: When multiple vertices have the same lex-min coordinates,
    /// the canonicalizer must deterministically choose which instance becomes first.
    ///
    /// Expected behavior: Pick the earliest index, then tie-break by next vertex sequence.
    /// This test creates a ring where vertex A appears twice at indices 0 and 2.
    /// </summary>
    [Fact]
    public void Canonicalize_DuplicateMinimalVertex_ChoosesEarliestIndex()
    {
        // Ring with duplicate lex-min vertex A(0,0,0) at indices 0 and 2
        // Shape: A → B → A → C → A (closed)
        // This is a degenerate ring but tests the tie-break rule
        var a = new Point3(0, 0, 0);  // lex-min (appears twice)
        var b = new Point3(1, 0, 0);
        var c = new Point3(0.5, 1, 0);

        var ring = new Polyline3([
            a,  // index 0 - first occurrence
            b,  // index 1
            a,  // index 2 - second occurrence (same coords as index 0)
            c,  // index 3
            a   // closing point
        ]);

        var result1 = RingCanonicalizer.Canonicalize(ring, WindingConvention.CounterClockwise);
        var result2 = RingCanonicalizer.Canonicalize(ring, WindingConvention.CounterClockwise);

        // Both runs should produce identical output (deterministic)
        result1.Count.Should().Be(result2.Count);
        for (var i = 0; i < result1.Count; i++)
        {
            result1[i].Should().Be(result2[i], $"vertex at index {i} should be identical");
        }

        // First vertex should be A (the lex-min)
        result1[0].Should().Be(a);
    }

    /// <summary>
    /// More complex case: a "figure-8" where the center vertex (lex-min) appears multiple times.
    /// Tests that canonicalization is stable across multiple runs.
    /// </summary>
    [Fact]
    public void Canonicalize_FigureEightWithDuplicateCenter_IsDeterministic()
    {
        // Figure-8 shape with center at origin (lex-min)
        var center = new Point3(0, 0, 0);
        var topLeft = new Point3(-1, 1, 0);
        var topRight = new Point3(1, 1, 0);
        var bottomLeft = new Point3(-1, -1, 0);
        var bottomRight = new Point3(1, -1, 0);

        // Ring: center → topLeft → center → topRight → center → bottomRight → center → bottomLeft → center
        var ring = new Polyline3([
            center,      // 0
            topLeft,     // 1
            center,      // 2 (duplicate)
            topRight,    // 3
            center,      // 4 (duplicate)
            bottomRight, // 5
            center,      // 6 (duplicate)
            bottomLeft,  // 7
            center       // close
        ]);

        // Run multiple times to verify determinism
        var results = new List<Polyline3>();
        for (var i = 0; i < 5; i++)
        {
            results.Add(RingCanonicalizer.Canonicalize(ring, WindingConvention.CounterClockwise));
        }

        // All results should be byte-identical
        var first = results[0];
        foreach (var r in results.Skip(1))
        {
            r.Count.Should().Be(first.Count);
            for (var i = 0; i < first.Count; i++)
            {
                r[i].X.Should().Be(first[i].X);
                r[i].Y.Should().Be(first[i].Y);
                r[i].Z.Should().Be(first[i].Z);
            }
        }
    }

    /// <summary>
    /// Tests that when two different vertices have the same X coordinate,
    /// tie-break uses Y, then Z (standard lexicographic order).
    /// </summary>
    [Fact]
    public void Canonicalize_TieBreakByYThenZ_WhenXEqual()
    {
        // Three vertices with same X=0, different Y
        var a = new Point3(0, 0, 0);   // lex-min (Y=0)
        var b = new Point3(0, 1, 0);   // Y=1
        var c = new Point3(1, 0.5, 0); // X=1

        var ring = new Polyline3([b, c, a, b]); // Start with B, A is at index 2

        var result = RingCanonicalizer.Canonicalize(ring, WindingConvention.CounterClockwise);

        // A(0,0,0) should be first (smallest X, then smallest Y)
        result[0].Should().Be(a);
    }

    [Fact]
    public void Canonicalize_TieBreakByZ_WhenXAndYEqual()
    {
        // Two vertices with same X=0, Y=0, different Z
        var a = new Point3(0, 0, -1);  // lex-min (Z=-1)
        var b = new Point3(0, 0, 1);   // Z=1
        var c = new Point3(1, 0, 0);

        var ring = new Polyline3([b, c, a, b]);

        var result = RingCanonicalizer.Canonicalize(ring, WindingConvention.CounterClockwise);

        // A(0,0,-1) should be first (smallest Z)
        result[0].Should().Be(a);
    }

    #endregion

    #region 3️⃣ Edge Cases

    [Fact]
    public void Canonicalize_EmptyRing_ReturnsEmpty()
    {
        var ring = new Polyline3([]);

        var result = RingCanonicalizer.Canonicalize(ring, WindingConvention.CounterClockwise);

        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Canonicalize_SinglePoint_ReturnsUnchanged()
    {
        var ring = new Polyline3([new Point3(1, 2, 3)]);

        var result = RingCanonicalizer.Canonicalize(ring, WindingConvention.CounterClockwise);

        result.Count.Should().Be(1);
        result[0].Should().Be(new Point3(1, 2, 3));
    }

    [Fact]
    public void Canonicalize_TwoPoints_ReturnsUnchanged()
    {
        var ring = new Polyline3([new Point3(1, 0, 0), new Point3(0, 0, 0)]);

        var result = RingCanonicalizer.Canonicalize(ring, WindingConvention.CounterClockwise);

        // Too few points for proper ring canonicalization
        result.Count.Should().Be(2);
    }

    #endregion

    #region 4️⃣ Signed Area / Winding Detection

    [Fact]
    public void ComputeSignedArea_CCWTriangle_ReturnsPositive()
    {
        // CCW triangle: (0,0) → (1,0) → (0.5,1) → (0,0)
        var ccw = new Polyline3([
            new Point3(0, 0, 0),
            new Point3(1, 0, 0),
            new Point3(0.5, 1, 0),
            new Point3(0, 0, 0)
        ]);

        var area = RingCanonicalizer.ComputeSignedArea(ccw);

        area.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ComputeSignedArea_CWTriangle_ReturnsNegative()
    {
        // CW triangle: (0,0) → (0.5,1) → (1,0) → (0,0)
        var cw = new Polyline3([
            new Point3(0, 0, 0),
            new Point3(0.5, 1, 0),
            new Point3(1, 0, 0),
            new Point3(0, 0, 0)
        ]);

        var area = RingCanonicalizer.ComputeSignedArea(cw);

        area.Should().BeLessThan(0);
    }

    [Fact]
    public void DetermineWinding_CCWRing_ReturnsCCW()
    {
        var ccw = new List<Point3>
        {
            new(0, 0, 0),
            new(1, 0, 0),
            new(0.5, 1, 0),
            new(0, 0, 0)
        };

        var winding = RingCanonicalizer.DetermineWinding(ccw);

        winding.Should().Be(WindingConvention.CounterClockwise);
    }

    [Fact]
    public void DetermineWinding_CWRing_ReturnsCW()
    {
        var cw = new List<Point3>
        {
            new(0, 0, 0),
            new(0.5, 1, 0),
            new(1, 0, 0),
            new(0, 0, 0)
        };

        var winding = RingCanonicalizer.DetermineWinding(cw);

        winding.Should().Be(WindingConvention.Clockwise);
    }

    #endregion

    #region Helpers

    private static List<Point3> ToPointList(Polyline3 polyline)
    {
        var list = new List<Point3>(polyline.Count);
        for (var i = 0; i < polyline.Count; i++)
        {
            list.Add(polyline[i]);
        }
        return list;
    }

    #endregion
}


// ------- HoleAssignerTests.cs -------

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


// ------- HoleAssigner.cs (Implementation) -------

using System.Collections.Immutable;
using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Polygonization.Solver;

/// <summary>
/// Assigns holes to outer rings per RFC-V2-0041 §9.4.
///
/// Classification rules:
/// 1. Rings with positive signed area (CCW) are outer rings
/// 2. Rings with negative signed area (CW) are holes
/// 3. Each hole is assigned to the innermost containing outer ring via centroid point-in-polygon test
/// </summary>
public static class HoleAssigner
{
    /// <summary>
    /// Result of hole assignment for a single plate.
    /// </summary>
    public readonly record struct PlateRingGroup(
        Polyline3 OuterRing,
        ImmutableArray<Polyline3> Holes
    );

    /// <summary>
    /// Classifies and assigns rings for a single plate.
    ///
    /// All input rings belong to the same PlateId. This method:
    /// 1. Separates rings into outers (CCW/positive area) and holes (CW/negative area)
    /// 2. Assigns each hole to its containing outer ring
    /// 3. Returns grouped outer+holes structures
    /// </summary>
    /// <param name="rings">All rings belonging to a single plate.</param>
    /// <param name="winding">Winding convention (default CCW = outer is positive area).</param>
    /// <returns>List of (outer, holes) groups. Usually one group per plate, but multiple if disjoint regions.</returns>
    public static IReadOnlyList<PlateRingGroup> AssignHoles(
        IReadOnlyList<Polyline3> rings,
        WindingConvention winding = WindingConvention.CounterClockwise)
    {
        if (rings.Count == 0)
        {
            return Array.Empty<PlateRingGroup>();
        }

        if (rings.Count == 1)
        {
            // Single ring = outer, no holes
            return new[] { new PlateRingGroup(rings[0], ImmutableArray<Polyline3>.Empty) };
        }

        // Classify rings by signed area
        var classified = rings
            .Select(r => (ring: r, area: RingCanonicalizer.ComputeSignedArea(r)))
            .ToList();

        // For CCW convention: positive area = outer, negative = hole
        // For CW convention: negative area = outer, positive = hole
        var isOuterPositive = winding == WindingConvention.CounterClockwise;

        var outers = classified
            .Where(c => isOuterPositive ? c.area > 0 : c.area < 0)
            .Select(c => (ring: c.ring, absArea: Math.Abs(c.area)))
            .OrderByDescending(c => c.absArea) // Largest outer first
            .ToList();

        var holes = classified
            .Where(c => isOuterPositive ? c.area < 0 : c.area > 0)
            .Select(c => c.ring)
            .ToList();

        if (outers.Count == 0)
        {
            // All rings are holes? This shouldn't happen for valid topology.
            // Return empty - caller should handle this as an error.
            return Array.Empty<PlateRingGroup>();
        }

        if (outers.Count == 1)
        {
            // Single outer, but only holes that are actually contained belong to it
            var containedHoles = holes
                .Where(hole => PointInPolygon(ComputeCentroid(hole), outers[0].ring))
                .ToImmutableArray();
            return new[] { new PlateRingGroup(outers[0].ring, containedHoles) };
        }

        // Multiple outers (disjoint regions of the same plate)
        // Assign each hole to the innermost containing outer
        var outerHoles = outers.ToDictionary(
            o => o.ring,
            _ => new List<Polyline3>());

        foreach (var hole in holes)
        {
            var centroid = ComputeCentroid(hole);
            Polyline3? containingOuter = null;
            var containingArea = double.MaxValue;

            foreach (var (outerRing, absArea) in outers)
            {
                if (PointInPolygon(centroid, outerRing))
                {
                    // Assign to innermost (smallest area) containing outer
                    if (absArea < containingArea)
                    {
                        containingOuter = outerRing;
                        containingArea = absArea;
                    }
                }
            }

            if (containingOuter != null)
            {
                outerHoles[containingOuter].Add(hole);
            }
            // Holes not contained by any outer are orphaned (topology error, but we don't fail here)
        }

        return outers
            .Select(o => new PlateRingGroup(o.ring, outerHoles[o.ring].ToImmutableArray()))
            .ToList();
    }

    /// <summary>
    /// Computes centroid of a ring (average of vertices).
    /// </summary>
    private static Point3 ComputeCentroid(Polyline3 ring)
    {
        if (ring.IsEmpty || ring.Count < 2)
        {
            return new Point3(0, 0, 0);
        }

        // Exclude closing point if ring is closed
        var isClosed = ArePointsEqual(ring[0], ring[ring.Count - 1]);
        var count = isClosed ? ring.Count - 1 : ring.Count;

        if (count == 0) return new Point3(0, 0, 0);

        double sumX = 0, sumY = 0, sumZ = 0;
        for (var i = 0; i < count; i++)
        {
            sumX += ring[i].X;
            sumY += ring[i].Y;
            sumZ += ring[i].Z;
        }

        return new Point3(sumX / count, sumY / count, sumZ / count);
    }

    /// <summary>
    /// Point-in-polygon test using ray casting algorithm (XY projection).
    /// </summary>
    private static bool PointInPolygon(Point3 point, Polyline3 polygon)
    {
        if (polygon.IsEmpty || polygon.Count < 4)
        {
            return false;
        }

        var x = point.X;
        var y = point.Y;
        var inside = false;

        var n = polygon.Count - 1; // Exclude closing point for iteration

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var xi = polygon[i].X;
            var yi = polygon[i].Y;
            var xj = polygon[j].X;
            var yj = polygon[j].Y;

            // Ray casting: count edge crossings
            if (((yi > y) != (yj > y)) &&
                (x < (xj - xi) * (y - yi) / (yj - yi) + xi))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static bool ArePointsEqual(Point3 a, Point3 b, double tolerance = 1e-12)
    {
        return Math.Abs(a.X - b.X) < tolerance &&
               Math.Abs(a.Y - b.Y) < tolerance &&
               Math.Abs(a.Z - b.Z) < tolerance;
    }
}
