using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonTriangulate
{
    /// <summary>
    /// Triangulates a simple polygon (no holes) using ear clipping.
    /// Returns an empty list if triangulation fails (e.g., self-intersections/degeneracy).
    /// </summary>
    public static IReadOnlyList<UGTriangle2> EarClip(UGPolygon2 polygon, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (polygon.Count < 3)
            return Array.Empty<UGTriangle2>();

        var verts = new List<UGPoint2>(polygon.Count);
        for (var i = 0; i < polygon.Count; i++)
        {
            var p = polygon[i];
            if (p.IsEmpty)
                return Array.Empty<UGTriangle2>();

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

        if (verts.Count < 3)
            return Array.Empty<UGTriangle2>();

        // Determine winding.
        var area = Polygon2.SignedArea(new UGPolygon2(verts));
        if (double.IsNaN(area) || Math.Abs(area) <= epsilon)
            return Array.Empty<UGTriangle2>();

        var isCCW = area > 0d;

        var indices = new List<int>(verts.Count);
        for (var i = 0; i < verts.Count; i++)
            indices.Add(i);

        var triangles = new List<UGTriangle2>(Math.Max(0, verts.Count - 2));

        var guard = 0;
        while (indices.Count > 3 && guard < verts.Count * verts.Count)
        {
            var earFound = false;

            for (var i = 0; i < indices.Count; i++)
            {
                var iPrev = indices[(i - 1 + indices.Count) % indices.Count];
                var iCurr = indices[i];
                var iNext = indices[(i + 1) % indices.Count];

                var a = verts[iPrev];
                var b = verts[iCurr];
                var c = verts[iNext];

                if (!IsConvex(a, b, c, isCCW, epsilon))
                    continue;

                if (TriangleAreaAbs(a, b, c) <= epsilon)
                    continue;

                // Check no other vertex lies inside (or on) the ear triangle.
                var anyInside = false;
                for (var k = 0; k < indices.Count; k++)
                {
                    var idx = indices[k];
                    if (idx == iPrev || idx == iCurr || idx == iNext)
                        continue;

                    if (PointInTriangle(verts[idx], a, b, c, epsilon))
                    {
                        anyInside = true;
                        break;
                    }
                }

                if (anyInside)
                    continue;

                triangles.Add(new UGTriangle2(a, b, c));
                indices.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
                return Array.Empty<UGTriangle2>();

            guard++;
        }

        if (indices.Count != 3)
            return Array.Empty<UGTriangle2>();

        {
            var a = verts[indices[0]];
            var b = verts[indices[1]];
            var c = verts[indices[2]];

            if (TriangleAreaAbs(a, b, c) <= epsilon)
                return Array.Empty<UGTriangle2>();

            triangles.Add(new UGTriangle2(a, b, c));
        }

        return triangles;
    }

    private static bool IsConvex(UGPoint2 a, UGPoint2 b, UGPoint2 c, bool polygonIsCCW, double epsilon)
    {
        var cross = Cross(b.X - a.X, b.Y - a.Y, c.X - b.X, c.Y - b.Y);
        if (double.IsNaN(cross) || double.IsInfinity(cross))
            return false;

        return polygonIsCCW ? cross > epsilon : cross < -epsilon;
    }

    private static bool PointInTriangle(UGPoint2 p, UGPoint2 a, UGPoint2 b, UGPoint2 c, double epsilon)
    {
        var s1 = Cross(b.X - a.X, b.Y - a.Y, p.X - a.X, p.Y - a.Y);
        var s2 = Cross(c.X - b.X, c.Y - b.Y, p.X - b.X, p.Y - b.Y);
        var s3 = Cross(a.X - c.X, a.Y - c.Y, p.X - c.X, p.Y - c.Y);

        var hasNeg = (s1 < -epsilon) || (s2 < -epsilon) || (s3 < -epsilon);
        var hasPos = (s1 > epsilon) || (s2 > epsilon) || (s3 > epsilon);
        return !(hasNeg && hasPos);
    }

    private static double TriangleAreaAbs(UGPoint2 a, UGPoint2 b, UGPoint2 c)
    {
        var cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        return 0.5d * Math.Abs(cross);
    }

    private static double Cross(double ax, double ay, double bx, double by) => (ax * by) - (ay * bx);

    private static bool NearlySame(UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy) <= (epsilon * epsilon);
    }
}
