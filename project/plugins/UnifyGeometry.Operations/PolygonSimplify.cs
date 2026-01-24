using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonSimplify
{
    /// <summary>
    /// Removes consecutive duplicate vertices and collinear vertices (within epsilon) and strips a closing duplicate.
    /// Returns <see cref="UGPolygon2.Empty"/> if fewer than 3 vertices remain.
    /// </summary>
    public static UGPolygon2 RemoveCollinearAndDuplicates(UGPolygon2 polygon, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (polygon.Count < 3)
            return UGPolygon2.Empty;

        var verts = new List<UGPoint2>(polygon.Count);
        for (var i = 0; i < polygon.Count; i++)
        {
            var p = polygon[i];
            if (p.IsEmpty)
                return UGPolygon2.Empty;
            verts.Add(p);
        }

        // Remove closing duplicate (first == last).
        if (verts.Count >= 2 && NearlySame(verts[0], verts[^1], epsilon))
            verts.RemoveAt(verts.Count - 1);

        // Remove consecutive duplicates.
        for (var i = verts.Count - 1; i >= 1; i--)
        {
            if (NearlySame(verts[i], verts[i - 1], epsilon))
                verts.RemoveAt(i);
        }

        if (verts.Count < 3)
            return UGPolygon2.Empty;

        // Remove collinear vertices. Iterate until stable.
        var changed = true;
        var guard = 0;
        while (changed && guard < verts.Count * verts.Count)
        {
            changed = false;
            for (var i = 0; i < verts.Count; i++)
            {
                var a = verts[(i - 1 + verts.Count) % verts.Count];
                var b = verts[i];
                var c = verts[(i + 1) % verts.Count];

                if (IsCollinear(a, b, c, epsilon))
                {
                    verts.RemoveAt(i);
                    changed = true;
                    break;
                }
            }

            if (verts.Count < 3)
                return UGPolygon2.Empty;

            guard++;
        }

        return new UGPolygon2(verts);
    }

    private static bool IsCollinear(UGPoint2 a, UGPoint2 b, UGPoint2 c, double epsilon)
    {
        // If b is extremely close to a or c, treat it as removable.
        if (NearlySame(a, b, epsilon) || NearlySame(b, c, epsilon))
            return true;

        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var bcx = c.X - b.X;
        var bcy = c.Y - b.Y;

        var cross = (abx * bcy) - (aby * bcx);
        if (double.IsNaN(cross) || double.IsInfinity(cross))
            return false;

        // Scale tolerance by edge lengths to be less sensitive for large coordinates.
        var ab2 = (abx * abx) + (aby * aby);
        var bc2 = (bcx * bcx) + (bcy * bcy);
        if (ab2 <= 0d || bc2 <= 0d)
            return true;

        var tol = epsilon * Math.Sqrt(ab2 * bc2);
        return Math.Abs(cross) <= tol;
    }

    private static bool NearlySame(UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy) <= (epsilon * epsilon);
    }
}
