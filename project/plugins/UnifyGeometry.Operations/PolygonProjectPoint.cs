using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonProjectPoint
{
    public static UGPoint2 ClosestPoint(UGPolygon2 polygon, UGPoint2 point, double epsilon = 1e-12)
        => ProjectPoint(polygon, point, epsilon).Point;

    public static UGPolygonProjectedPoint2 ProjectPoint(UGPolygon2 polygon, UGPoint2 point, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (point.IsEmpty)
            return UGPolygonProjectedPoint2.Empty;

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        var verts = CollectVertices(polygon, epsilon);
        if (verts.Count < 3)
            return UGPolygonProjectedPoint2.Empty;

        var bestD2 = double.PositiveInfinity;
        var bestPoint = UGPoint2.Empty;
        var bestEdgeIndex = -1;
        var bestT = double.NaN;

        for (var i = 0; i < verts.Count; i++)
        {
            var a = verts[i];
            var b = verts[(i + 1) % verts.Count];

            var proj = ProjectToSegment(point, a, b);
            if (proj.DistanceSquared < bestD2)
            {
                bestD2 = proj.DistanceSquared;
                bestPoint = proj.Point;
                bestEdgeIndex = i;
                bestT = proj.T;
            }
        }

        if (bestEdgeIndex < 0 || bestPoint.IsEmpty || double.IsNaN(bestD2) || double.IsInfinity(bestD2))
            return UGPolygonProjectedPoint2.Empty;

        var sign = 0;
        var eps2 = epsilon * epsilon;
        if (bestD2 > eps2)
            sign = Polygon2.ContainsPoint(new UGPolygon2(verts), point, epsilon) ? -1 : 1;

        return new UGPolygonProjectedPoint2(
            Point: bestPoint,
            EdgeIndex: bestEdgeIndex,
            EdgeT: bestT,
            DistanceSquared: bestD2,
            ContainsSign: sign);
    }

    private static SegmentProjection ProjectToSegment(UGPoint2 p, UGPoint2 a, UGPoint2 b)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var apx = p.X - a.X;
        var apy = p.Y - a.Y;

        var denom = (abx * abx) + (aby * aby);
        if (denom <= 0d || double.IsNaN(denom) || double.IsInfinity(denom))
            return new SegmentProjection(Point: a, T: 0d, DistanceSquared: DistanceSquared(p, a));

        var t = ((apx * abx) + (apy * aby)) / denom;
        if (double.IsNaN(t) || double.IsInfinity(t))
            return new SegmentProjection(Point: a, T: 0d, DistanceSquared: DistanceSquared(p, a));

        if (t <= 0d)
            return new SegmentProjection(Point: a, T: 0d, DistanceSquared: DistanceSquared(p, a));

        if (t >= 1d)
            return new SegmentProjection(Point: b, T: 1d, DistanceSquared: DistanceSquared(p, b));

        var q = new UGPoint2(a.X + (abx * t), a.Y + (aby * t));
        return new SegmentProjection(Point: q, T: t, DistanceSquared: DistanceSquared(p, q));
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

    private static double DistanceSquared(UGPoint2 a, UGPoint2 b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy);
    }

    private readonly record struct SegmentProjection(UGPoint2 Point, double T, double DistanceSquared);
}

public readonly record struct UGPolygonProjectedPoint2(
    UGPoint2 Point,
    int EdgeIndex,
    double EdgeT,
    double DistanceSquared,
    int ContainsSign)
{
    public bool IsEmpty => EdgeIndex < 0 || Point.IsEmpty;

    public static UGPolygonProjectedPoint2 Empty =>
        new(UGPoint2.Empty, -1, double.NaN, double.NaN, 0);
}
