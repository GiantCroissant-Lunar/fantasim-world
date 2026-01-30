using FantaSim.Geosphere.Plate.Polygonization.Contracts.Products;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Polygonization.Solver;

/// <summary>
/// Canonicalizes polygon rings per RFC-V2-0041 §9.3.
///
/// Canonicalization ensures deterministic ring representation:
/// 1. Rotate ring so lexicographically minimum vertex is first
/// 2. Enforce consistent winding convention (CCW for outers, CW for holes)
/// </summary>
public static class RingCanonicalizer
{
    /// <summary>
    /// Canonicalizes a ring by rotating to lex-min vertex and enforcing winding.
    /// </summary>
    /// <param name="ring">The input ring (closed polyline, first == last).</param>
    /// <param name="targetWinding">Desired winding convention.</param>
    /// <returns>Canonicalized ring.</returns>
    public static Polyline3 Canonicalize(Polyline3 ring, WindingConvention targetWinding)
    {
        if (ring.IsEmpty || ring.Count < 4)
        {
            // Need at least 3 unique vertices + closing point
            return ring;
        }

        // Work with unique vertices (exclude closing duplicate)
        var isClosed = ArePointsEqual(ring[0], ring[ring.Count - 1]);
        var uniqueCount = isClosed ? ring.Count - 1 : ring.Count;

        if (uniqueCount < 3)
        {
            return ring;
        }

        // Step 1: Find lexicographically minimum vertex index
        var minIndex = FindLexMinVertexIndex(ring, uniqueCount);

        // Step 2: Rotate ring so min vertex is first
        var rotated = RotateRing(ring, minIndex, uniqueCount);

        // Step 3: Compute current winding and reverse if needed
        var currentWinding = DetermineWinding(rotated);
        var needsReverse = (targetWinding == WindingConvention.CounterClockwise && currentWinding == WindingConvention.Clockwise) ||
                          (targetWinding == WindingConvention.Clockwise && currentWinding == WindingConvention.CounterClockwise);

        if (needsReverse)
        {
            rotated = ReverseRing(rotated);
        }

        return new Polyline3(rotated);
    }

    /// <summary>
    /// Finds the index of the lexicographically minimum vertex.
    /// Compares by X, then Y, then Z.
    /// </summary>
    private static int FindLexMinVertexIndex(Polyline3 ring, int count)
    {
        var minIndex = 0;
        var minPoint = ring[0];

        for (var i = 1; i < count; i++)
        {
            var p = ring[i];
            if (CompareLexicographic(p, minPoint) < 0)
            {
                minIndex = i;
                minPoint = p;
            }
        }

        return minIndex;
    }

    /// <summary>
    /// Lexicographic comparison: X, then Y, then Z.
    /// </summary>
    private static int CompareLexicographic(Point3 a, Point3 b)
    {
        var cmpX = a.X.CompareTo(b.X);
        if (cmpX != 0) return cmpX;

        var cmpY = a.Y.CompareTo(b.Y);
        if (cmpY != 0) return cmpY;

        return a.Z.CompareTo(b.Z);
    }

    /// <summary>
    /// Rotates ring so vertex at startIndex becomes first.
    /// Returns a closed ring (first == last).
    /// </summary>
    private static List<Point3> RotateRing(Polyline3 ring, int startIndex, int uniqueCount)
    {
        var result = new List<Point3>(uniqueCount + 1);

        for (var i = 0; i < uniqueCount; i++)
        {
            var srcIndex = (startIndex + i) % uniqueCount;
            result.Add(ring[srcIndex]);
        }

        // Close the ring
        result.Add(result[0]);

        return result;
    }

    /// <summary>
    /// Reverses ring order while keeping it closed (first == last).
    /// Preserves the first vertex position.
    /// </summary>
    private static List<Point3> ReverseRing(List<Point3> ring)
    {
        // For a ring [A, B, C, D, A], reversed keeping A first is [A, D, C, B, A]
        var uniqueCount = ring.Count - 1;
        var result = new List<Point3>(ring.Count) { ring[0] };

        for (var i = uniqueCount - 1; i >= 1; i--)
        {
            result.Add(ring[i]);
        }

        // Close
        result.Add(ring[0]);

        return result;
    }

    /// <summary>
    /// Determines the winding convention of a ring using signed area.
    /// </summary>
    public static WindingConvention DetermineWinding(IReadOnlyList<Point3> ring)
    {
        var signedArea = ComputeSignedArea(ring);
        // Positive area = CCW, Negative = CW (with standard coordinate system)
        return signedArea >= 0 ? WindingConvention.CounterClockwise : WindingConvention.Clockwise;
    }

    /// <summary>
    /// Computes signed area of a ring (shoelace formula, XY projection).
    /// Positive = CCW, Negative = CW.
    /// </summary>
    public static double ComputeSignedArea(IReadOnlyList<Point3> points)
    {
        if (points.Count < 3) return 0;

        double sum = 0;
        for (var i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[i];
            var p1 = points[i + 1];
            sum += (p1.X - p0.X) * (p1.Y + p0.Y);
        }

        return -sum / 2.0; // Negate for standard CCW=positive convention
    }

    /// <summary>
    /// Computes signed area of a Polyline3 (shoelace formula, XY projection).
    /// Positive = CCW, Negative = CW.
    /// </summary>
    public static double ComputeSignedArea(Polyline3 ring)
    {
        if (ring.Count < 3) return 0;

        double sum = 0;
        for (var i = 0; i < ring.Count - 1; i++)
        {
            var p0 = ring[i];
            var p1 = ring[i + 1];
            sum += (p1.X - p0.X) * (p1.Y + p0.Y);
        }

        return -sum / 2.0; // Negate for standard CCW=positive convention
    }

    private static bool ArePointsEqual(Point3 a, Point3 b, double tolerance = 1e-12)
    {
        return Math.Abs(a.X - b.X) < tolerance &&
               Math.Abs(a.Y - b.Y) < tolerance &&
               Math.Abs(a.Z - b.Z) < tolerance;
    }
}
