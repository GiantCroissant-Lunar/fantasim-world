using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonSelfIntersections
{
    public static IReadOnlyList<UGPolygonSelfIntersection2> Find(UGPolygon2 polygon, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (polygon.Count < 3)
            return Array.Empty<UGPolygonSelfIntersection2>();

        // Copy vertices; reject invalid.
        var verts = new List<UGPoint2>(polygon.Count);
        for (var i = 0; i < polygon.Count; i++)
        {
            var p = polygon[i];
            if (p.IsEmpty)
                return Array.Empty<UGPolygonSelfIntersection2>();
            verts.Add(p);
        }

        // Strip closing duplicate.
        if (verts.Count >= 2 && NearlySame(verts[0], verts[^1], epsilon))
            verts.RemoveAt(verts.Count - 1);

        // Remove consecutive duplicates (zero-length edges).
        for (var i = verts.Count - 1; i >= 1; i--)
        {
            if (NearlySame(verts[i], verts[i - 1], epsilon))
                verts.RemoveAt(i);
        }

        if (verts.Count < 3)
            return Array.Empty<UGPolygonSelfIntersection2>();

        var edgeCount = verts.Count;
        var hits = new List<UGPolygonSelfIntersection2>();

        for (var i = 0; i < edgeCount; i++)
        {
            var a0 = verts[i];
            var a1 = verts[(i + 1) % edgeCount];
            var segA = new UGSegment2(a0, a1);

            for (var j = i + 1; j < edgeCount; j++)
            {
                if (AreAdjacentEdges(i, j, edgeCount))
                    continue;

                var b0 = verts[j];
                var b1 = verts[(j + 1) % edgeCount];
                var segB = new UGSegment2(b0, b1);

                var inter = SegmentIntersection2.Intersect(segA, segB, epsilon);
                if (inter.Kind == UGSegmentIntersectionKind.None)
                    continue;

                if (inter.Kind == UGSegmentIntersectionKind.Point)
                {
                    hits.Add(new UGPolygonSelfIntersection2(
                        Kind: UGSegmentIntersectionKind.Point,
                        Point: inter.Point,
                        OverlapStart: UGPoint2.Empty,
                        OverlapEnd: UGPoint2.Empty,
                        EdgeIndexA: i,
                        EdgeIndexB: j));
                    continue;
                }

                // Overlap
                hits.Add(new UGPolygonSelfIntersection2(
                    Kind: UGSegmentIntersectionKind.Overlap,
                    Point: UGPoint2.Empty,
                    OverlapStart: inter.OverlapStart,
                    OverlapEnd: inter.OverlapEnd,
                    EdgeIndexA: i,
                    EdgeIndexB: j));
            }
        }

        // Dedup point hits by location; keep overlaps as-is.
        if (hits.Count <= 1)
            return hits;

        var dedup = new List<UGPolygonSelfIntersection2>(hits.Count);
        foreach (var h in hits)
        {
            if (h.Kind != UGSegmentIntersectionKind.Point)
            {
                dedup.Add(h);
                continue;
            }

            var exists = false;
            for (var k = 0; k < dedup.Count; k++)
            {
                if (dedup[k].Kind == UGSegmentIntersectionKind.Point && NearlySame(dedup[k].Point, h.Point, epsilon))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                dedup.Add(h);
        }

        return dedup;
    }

    private static bool AreAdjacentEdges(int a, int b, int edgeCount)
    {
        if (a == b)
            return true;

        if (Math.Abs(a - b) == 1)
            return true;

        // First and last are adjacent in a closed polygon.
        if ((a == 0 && b == edgeCount - 1) || (b == 0 && a == edgeCount - 1))
            return true;

        return false;
    }

    private static bool NearlySame(UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy) <= (epsilon * epsilon);
    }
}

public readonly record struct UGPolygonSelfIntersection2(
    UGSegmentIntersectionKind Kind,
    UGPoint2 Point,
    UGPoint2 OverlapStart,
    UGPoint2 OverlapEnd,
    int EdgeIndexA,
    int EdgeIndexB);
