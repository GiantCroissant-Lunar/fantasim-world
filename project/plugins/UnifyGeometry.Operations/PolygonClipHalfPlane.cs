using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonClipHalfPlane
{
    /// <summary>
    /// Clips a simple polygon to the half-plane defined by (p - pointOnBoundary)·inwardNormal >= 0.
    /// </summary>
    public static UGPolygon2 ToHalfPlane(UGPolygon2 polygon, UGPoint2 pointOnBoundary, UGPoint2 inwardNormal)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (pointOnBoundary.IsEmpty || inwardNormal.IsEmpty)
            return UGPolygon2.Empty;

        var nx = inwardNormal.X;
        var ny = inwardNormal.Y;
        if (double.IsNaN(nx) || double.IsNaN(ny) || double.IsInfinity(nx) || double.IsInfinity(ny))
            throw new ArgumentOutOfRangeException(nameof(inwardNormal), "Normal must be finite.");

        if (Math.Abs(nx) <= 0d && Math.Abs(ny) <= 0d)
            throw new ArgumentOutOfRangeException(nameof(inwardNormal), "Normal must not be zero.");

        if (polygon.Count < 3)
            return UGPolygon2.Empty;

        for (var i = 0; i < polygon.Count; i++)
        {
            if (polygon[i].IsEmpty)
                return UGPolygon2.Empty;
        }

        // Sutherland–Hodgman clipping for a single half-plane.
        var output = new List<UGPoint2>(polygon.Count + 2);

        var prev = polygon[^1];
        var prevVal = Eval(prev);
        var prevIn = prevVal >= 0d;

        for (var i = 0; i < polygon.Count; i++)
        {
            var curr = polygon[i];
            var currVal = Eval(curr);
            var currIn = currVal >= 0d;

            if (prevIn && currIn)
            {
                AddPointDedup(output, curr);
            }
            else if (prevIn && !currIn)
            {
                // leaving: add intersection
                if (TryIntersect(prev, prevVal, curr, currVal, out var inter))
                    AddPointDedup(output, inter);
            }
            else if (!prevIn && currIn)
            {
                // entering: add intersection then current
                if (TryIntersect(prev, prevVal, curr, currVal, out var inter))
                    AddPointDedup(output, inter);
                AddPointDedup(output, curr);
            }

            prev = curr;
            prevVal = currVal;
            prevIn = currIn;
        }

        // Remove duplicate closing point if present.
        if (output.Count >= 2 && ApproximatelyEqual(output[0], output[^1]))
            output.RemoveAt(output.Count - 1);

        return output.Count >= 3 ? new UGPolygon2(output) : UGPolygon2.Empty;

        double Eval(UGPoint2 p)
        {
            var dx = p.X - pointOnBoundary.X;
            var dy = p.Y - pointOnBoundary.Y;
            return (dx * nx) + (dy * ny);
        }
    }

    private static bool TryIntersect(UGPoint2 a, double fa, UGPoint2 b, double fb, out UGPoint2 intersection)
    {
        var denom = fa - fb;
        if (Math.Abs(denom) < 1e-15)
        {
            intersection = UGPoint2.Empty;
            return false;
        }

        var t = fa / denom; // intersection on segment a->b
        if (t < 0d) t = 0d;
        if (t > 1d) t = 1d;

        var ix = a.X + ((b.X - a.X) * t);
        var iy = a.Y + ((b.Y - a.Y) * t);
        intersection = new UGPoint2(ix, iy);
        return true;
    }

    private static void AddPointDedup(List<UGPoint2> points, UGPoint2 p)
    {
        if (points.Count == 0 || !ApproximatelyEqual(points[^1], p))
            points.Add(p);
    }

    private static bool ApproximatelyEqual(UGPoint2 a, UGPoint2 b)
        => Math.Abs(a.X - b.X) <= 1e-12 && Math.Abs(a.Y - b.Y) <= 1e-12;
}
