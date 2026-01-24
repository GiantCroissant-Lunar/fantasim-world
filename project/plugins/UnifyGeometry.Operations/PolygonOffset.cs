using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonOffset
{
    /// <summary>
    /// Offsets a simple polygon by <paramref name="distance"/> in its outward direction.
    /// Positive distance expands; negative contracts. Output is a best-effort simple ring and may self-intersect for complex inputs.
    /// </summary>
    public static UGPolygon2 ByDistance(
        UGPolygon2 polygon,
        double distance,
        double miterLimit = 10d,
        double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (double.IsNaN(distance) || double.IsInfinity(distance))
            throw new ArgumentOutOfRangeException(nameof(distance), "distance must be finite.");

        if (double.IsNaN(miterLimit) || double.IsInfinity(miterLimit) || miterLimit <= 0d)
            throw new ArgumentOutOfRangeException(nameof(miterLimit), "miterLimit must be finite and > 0.");

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (polygon.Count < 3)
            return UGPolygon2.Empty;

        if (distance == 0d)
            return polygon;

        var simp = PolygonSimplify.RemoveCollinearAndDuplicates(polygon, epsilon);
        if (simp.IsEmpty)
            return UGPolygon2.Empty;

        var area = Polygon2.SignedArea(simp);
        if (double.IsNaN(area) || Math.Abs(area) <= epsilon)
            return UGPolygon2.Empty;

        var isCCW = area > 0d;

        // For CCW polygons, interior is left of each directed edge; outward is right normal.
        // For CW polygons, outward is left normal.
        var wantRightNormal = isCCW;

        var outPoints = new List<UGPoint2>(simp.Count * 2);

        for (var i = 0; i < simp.Count; i++)
        {
            var prev = simp[(i - 1 + simp.Count) % simp.Count];
            var curr = simp[i];
            var next = simp[(i + 1) % simp.Count];

            var e1 = new UGPoint2(curr.X - prev.X, curr.Y - prev.Y);
            var e2 = new UGPoint2(next.X - curr.X, next.Y - curr.Y);

            var len1 = Math.Sqrt((e1.X * e1.X) + (e1.Y * e1.Y));
            var len2 = Math.Sqrt((e2.X * e2.X) + (e2.Y * e2.Y));
            if (len1 <= epsilon || len2 <= epsilon)
                continue;

            var d1x = e1.X / len1;
            var d1y = e1.Y / len1;
            var d2x = e2.X / len2;
            var d2y = e2.Y / len2;

            var n1 = wantRightNormal ? RightNormal(d1x, d1y) : LeftNormal(d1x, d1y);
            var n2 = wantRightNormal ? RightNormal(d2x, d2y) : LeftNormal(d2x, d2y);

            // Offset the two adjacent edge supporting lines.
            var p1a = new UGPoint2(prev.X + (n1.X * distance), prev.Y + (n1.Y * distance));
            var p1b = new UGPoint2(curr.X + (n1.X * distance), curr.Y + (n1.Y * distance));

            var p2a = new UGPoint2(curr.X + (n2.X * distance), curr.Y + (n2.Y * distance));
            var p2b = new UGPoint2(next.X + (n2.X * distance), next.Y + (n2.Y * distance));

            if (!TryIntersectLines(p1a, p1b, p2a, p2b, epsilon, out var miter))
            {
                // Parallel-ish: bevel join
                AddPointDedup(outPoints, p1b, epsilon);
                AddPointDedup(outPoints, p2a, epsilon);
                continue;
            }

            var miterVecX = miter.X - (curr.X + ((n1.X + n2.X) * 0.5d * distance));
            var miterVecY = miter.Y - (curr.Y + ((n1.Y + n2.Y) * 0.5d * distance));
            var miterLen = Math.Sqrt((miterVecX * miterVecX) + (miterVecY * miterVecY));

            // Compare against |distance| to apply miter limit.
            if (miterLen > miterLimit * Math.Abs(distance))
            {
                AddPointDedup(outPoints, p1b, epsilon);
                AddPointDedup(outPoints, p2a, epsilon);
                continue;
            }

            AddPointDedup(outPoints, miter, epsilon);
        }

        if (outPoints.Count < 3)
            return UGPolygon2.Empty;

        // Remove closing duplicate if present.
        if (outPoints.Count >= 2 && NearlySame(outPoints[0], outPoints[^1], epsilon))
            outPoints.RemoveAt(outPoints.Count - 1);

        return outPoints.Count >= 3 ? new UGPolygon2(outPoints) : UGPolygon2.Empty;
    }

    private static void AddPointDedup(List<UGPoint2> points, UGPoint2 p, double epsilon)
    {
        if (points.Count == 0 || !NearlySame(points[^1], p, epsilon))
            points.Add(p);
    }

    private static bool TryIntersectLines(
        UGPoint2 a0,
        UGPoint2 a1,
        UGPoint2 b0,
        UGPoint2 b1,
        double epsilon,
        out UGPoint2 intersection)
    {
        // Solve for intersection of infinite lines:
        // a0 + t(a1-a0) intersects b0 + u(b1-b0)
        var ax = a1.X - a0.X;
        var ay = a1.Y - a0.Y;
        var bx = b1.X - b0.X;
        var by = b1.Y - b0.Y;

        var denom = (ax * by) - (ay * bx);
        if (double.IsNaN(denom) || double.IsInfinity(denom) || Math.Abs(denom) <= epsilon)
        {
            intersection = UGPoint2.Empty;
            return false;
        }

        var cx = b0.X - a0.X;
        var cy = b0.Y - a0.Y;

        var t = ((cx * by) - (cy * bx)) / denom;
        if (double.IsNaN(t) || double.IsInfinity(t))
        {
            intersection = UGPoint2.Empty;
            return false;
        }

        intersection = new UGPoint2(a0.X + (ax * t), a0.Y + (ay * t));
        return true;
    }

    private static UGPoint2 LeftNormal(double dx, double dy) => new(-dy, dx);

    private static UGPoint2 RightNormal(double dx, double dy) => new(dy, -dx);

    private static bool NearlySame(UGPoint2 a, UGPoint2 b, double epsilon)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy) <= (epsilon * epsilon);
    }
}
