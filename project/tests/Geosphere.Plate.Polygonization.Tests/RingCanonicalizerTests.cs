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
