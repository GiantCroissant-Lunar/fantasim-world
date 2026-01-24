using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonValidateSimple
{
    /// <summary>
    /// Returns true if the polygon is simple (no self-intersections and no repeated vertices within epsilon).
    /// This is a best-effort validator intended for guarding downstream ops like triangulation/offset/booleans.
    /// </summary>
    public static bool IsSimple(UGPolygon2 polygon, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (polygon.Count < 3)
            return false;

        // Only do light normalization for validation (do NOT remove collinear vertices):
        // removing collinear vertices can "repair" invalid polygons that reuse vertices.
        var verts = new List<UGPoint2>(polygon.Count);
        for (var i = 0; i < polygon.Count; i++)
        {
            var p = polygon[i];
            if (p.IsEmpty)
                return false;
            verts.Add(p);
        }

        if (verts.Count >= 2 && NearlySame(verts[0], verts[^1], epsilon))
            verts.RemoveAt(verts.Count - 1);

        for (var i = verts.Count - 1; i >= 1; i--)
        {
            if (NearlySame(verts[i], verts[i - 1], epsilon))
                verts.RemoveAt(i);
        }

        if (verts.Count < 3)
            return false;

        // Reject near-duplicate vertices anywhere (touching at a vertex counts as non-simple here).
        for (var i = 0; i < verts.Count; i++)
        {
            var a = verts[i];
            for (var j = i + 1; j < verts.Count; j++)
            {
                var b = verts[j];
                if (NearlySame(a, b, epsilon))
                    return false;
            }
        }

        var hits = PolygonSelfIntersections.Find(new UGPolygon2(verts), epsilon);
        if (hits.Count == 0)
            return true;

        // Any overlap or non-adjacent intersection makes it non-simple.
        return false;
    }

    private static bool NearlySame(UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy) <= (epsilon * epsilon);
    }
}
