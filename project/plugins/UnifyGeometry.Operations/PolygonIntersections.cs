using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonIntersections
{
    /// <summary>
    /// Finds intersection points between the boundary edges of two polygons (treated as closed rings).
    /// Overlapping edges are represented by their overlap endpoints.
    /// Returns a deduplicated point set (within <paramref name="epsilon"/>).
    /// </summary>
    public static IReadOnlyList<UGPoint2> IntersectBoundaries(
        UGPolygon2 a,
        UGPolygon2 b,
        double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        var av = CollectVertices(a, epsilon);
        var bv = CollectVertices(b, epsilon);
        if (av.Count < 3 || bv.Count < 3)
            return Array.Empty<UGPoint2>();

        var hits = new List<UGPoint2>();

        for (var i = 0; i < av.Count; i++)
        {
            var a0 = av[i];
            var a1 = av[(i + 1) % av.Count];
            var aSeg = new UGSegment2(a0, a1);

            for (var j = 0; j < bv.Count; j++)
            {
                var b0 = bv[j];
                var b1 = bv[(j + 1) % bv.Count];
                var bSeg = new UGSegment2(b0, b1);

                var inter = SegmentIntersection2.Intersect(aSeg, bSeg, epsilon);
                if (inter.Kind == UGSegmentIntersectionKind.Point)
                {
                    hits.Add(inter.Point);
                }
                else if (inter.Kind == UGSegmentIntersectionKind.Overlap)
                {
                    hits.Add(inter.OverlapStart);
                    if (!NearlySame(inter.OverlapStart, inter.OverlapEnd, epsilon))
                        hits.Add(inter.OverlapEnd);
                }
            }
        }

        if (hits.Count <= 1)
            return hits;

        var dedup = new List<UGPoint2>();
        foreach (var p in hits)
        {
            var exists = false;
            for (var k = 0; k < dedup.Count; k++)
            {
                if (NearlySame(dedup[k], p, epsilon))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                dedup.Add(p);
        }

        return dedup;
    }

    private static List<UGPoint2> CollectVertices(UGPolygon2 polygon, double epsilon)
    {
        if (polygon.Count < 3)
            return new List<UGPoint2>(0);

        var verts = new List<UGPoint2>(polygon.Count);
        for (var i = 0; i < polygon.Count; i++)
        {
            var p = polygon[i];
            if (p.IsEmpty)
                return new List<UGPoint2>(0);

            verts.Add(p);
        }

        // Drop closing duplicate if present.
        if (verts.Count >= 2 && NearlySame(verts[0], verts[^1], epsilon))
            verts.RemoveAt(verts.Count - 1);

        // Remove consecutive duplicates.
        for (var i = verts.Count - 1; i >= 1; i--)
        {
            if (NearlySame(verts[i], verts[i - 1], epsilon))
                verts.RemoveAt(i);
        }

        return verts;
    }

    private static bool NearlySame(UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy) <= (epsilon * epsilon);
    }
}
