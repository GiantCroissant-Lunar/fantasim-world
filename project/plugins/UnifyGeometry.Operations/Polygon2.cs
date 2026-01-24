using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class Polygon2
{
    public static double SignedArea(UGPolygon2 polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (polygon.Count < 3)
            return 0d;

        double sum = 0d;
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            if (a.IsEmpty || b.IsEmpty)
                return double.NaN;

            sum += (a.X * b.Y) - (b.X * a.Y);
        }

        return 0.5d * sum;
    }

    public static bool IsClockwise(UGPolygon2 polygon)
    {
        var area = SignedArea(polygon);
        if (double.IsNaN(area))
            return false;

        return area < 0d;
    }

    /// <summary>
    /// Point-in-polygon test using ray casting. Points on the boundary are considered inside.
    /// </summary>
    public static bool ContainsPoint(UGPolygon2 polygon, UGPoint2 point, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (point.IsEmpty)
            return false;

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (polygon.Count < 3)
            return false;

        // Boundary check + ray casting.
        var inside = false;
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            if (a.IsEmpty || b.IsEmpty)
                return false;

            if (PointOnSegment(point, a, b, epsilon))
                return true;

            // Ray cast to +X. Standard implementation with Y comparisons.
            var cond1 = a.Y > point.Y;
            var cond2 = b.Y > point.Y;
            if (cond1 == cond2)
                continue;

            var xAtY = a.X + ((b.X - a.X) * ((point.Y - a.Y) / (b.Y - a.Y)));
            if (xAtY > point.X)
                inside = !inside;
        }

        return inside;
    }

    private static bool PointOnSegment(UGPoint2 p, UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var apx = p.X - a.X;
        var apy = p.Y - a.Y;

        var cross = (abx * apy) - (aby * apx);
        if (double.IsNaN(cross) || double.IsInfinity(cross))
            return false;

        if (Math.Abs(cross) > epsilon)
            return false;

        var dot = (apx * abx) + (apy * aby);
        if (dot < -epsilon)
            return false;

        var ab2 = (abx * abx) + (aby * aby);
        if (dot > ab2 + epsilon)
            return false;

        return true;
    }
}
