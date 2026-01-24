using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class SegmentIntersection2
{
    public static UGSegmentIntersection2 Intersect(UGSegment2 a, UGSegment2 b, double epsilon = 1e-12)
    {
        if (a.IsEmpty || b.IsEmpty)
            return UGSegmentIntersection2.None;

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        var ax = a.Start.X;
        var ay = a.Start.Y;
        var bx = a.End.X;
        var by = a.End.Y;

        var cx = b.Start.X;
        var cy = b.Start.Y;
        var dx = b.End.X;
        var dy = b.End.Y;

        // a(t) = A + t(B-A), b(u) = C + u(D-C)
        var rX = bx - ax;
        var rY = by - ay;
        var sX = dx - cx;
        var sY = dy - cy;

        var qpx = cx - ax;
        var qpy = cy - ay;

        var rxs = Cross(rX, rY, sX, sY);
        var qpxr = Cross(qpx, qpy, rX, rY);

        if (NearlyZero(rxs, epsilon))
        {
            // Parallel
            if (!NearlyZero(qpxr, epsilon))
                return UGSegmentIntersection2.None;

            // Collinear: project onto r.
            var r2 = (rX * rX) + (rY * rY);
            if (NearlyZero(r2, epsilon))
            {
                // a is a point
                var d2 = DistanceSquared(a.Start, b.Start);
                if (d2 <= epsilon * epsilon)
                    return UGSegmentIntersection2.AtPoint(a.Start, 0d, 0d);

                // Check if point lies on b
                var projOnB = ProjectPointToSegment(a.Start, b, epsilon);
                return projOnB.HasValue
                    ? UGSegmentIntersection2.AtPoint(a.Start, 0d, projOnB.Value)
                    : UGSegmentIntersection2.None;
            }

            var t0 = Dot(qpx, qpy, rX, rY) / r2;
            var t1 = t0 + (Dot(sX, sY, rX, rY) / r2);

            var minT = Math.Min(t0, t1);
            var maxT = Math.Max(t0, t1);

            var i0 = Clamp01(minT);
            var i1 = Clamp01(maxT);

            if (i1 < -epsilon || i0 > 1d + epsilon)
                return UGSegmentIntersection2.None;

            // Clamp intersection interval to [0,1]
            i0 = Clamp01(i0);
            i1 = Clamp01(i1);

            if (NearlyZero(i1 - i0, epsilon))
            {
                var p = new UGPoint2(ax + (rX * i0), ay + (rY * i0));
                var u = ProjectPointToSegment(p, b, epsilon) ?? 0d;
                return UGSegmentIntersection2.AtPoint(p, i0, u);
            }

            var p0 = new UGPoint2(ax + (rX * i0), ay + (rY * i0));
            var p1 = new UGPoint2(ax + (rX * i1), ay + (rY * i1));
            return UGSegmentIntersection2.OverlapSegment(p0, p1, i0, i1);
        }

        // Proper intersection
        var t = Cross(qpx, qpy, sX, sY) / rxs;
        var u2 = Cross(qpx, qpy, rX, rY) / rxs;

        if (t < -epsilon || t > 1d + epsilon || u2 < -epsilon || u2 > 1d + epsilon)
            return UGSegmentIntersection2.None;

        var tc = Clamp01(t);
        var uc = Clamp01(u2);

        var ip = new UGPoint2(ax + (rX * tc), ay + (rY * tc));
        return UGSegmentIntersection2.AtPoint(ip, tc, uc);
    }

    private static double? ProjectPointToSegment(UGPoint2 p, UGSegment2 s, double epsilon)
    {
        var ax = s.Start.X;
        var ay = s.Start.Y;
        var bx = s.End.X;
        var by = s.End.Y;

        var abx = bx - ax;
        var aby = by - ay;
        var apx = p.X - ax;
        var apy = p.Y - ay;

        var denom = (abx * abx) + (aby * aby);
        if (NearlyZero(denom, epsilon))
        {
            var d2 = DistanceSquared(p, s.Start);
            return d2 <= epsilon * epsilon ? 0d : null;
        }

        var t = ((apx * abx) + (apy * aby)) / denom;
        if (t < -epsilon || t > 1d + epsilon)
            return null;

        var tc = Clamp01(t);
        var q = new UGPoint2(ax + (abx * tc), ay + (aby * tc));
        var d2q = DistanceSquared(p, q);
        return d2q <= epsilon * epsilon ? tc : null;
    }

    private static double Cross(double ax, double ay, double bx, double by) => (ax * by) - (ay * bx);

    private static double Dot(double ax, double ay, double bx, double by) => (ax * bx) + (ay * by);

    private static bool NearlyZero(double v, double epsilon) => Math.Abs(v) <= epsilon;

    private static double Clamp01(double t) => t < 0d ? 0d : (t > 1d ? 1d : t);

    private static double DistanceSquared(UGPoint2 a, UGPoint2 b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy);
    }
}

public enum UGSegmentIntersectionKind
{
    None = 0,
    Point = 1,
    Overlap = 2,
}

public readonly record struct UGSegmentIntersection2(
    UGSegmentIntersectionKind Kind,
    UGPoint2 Point,
    UGPoint2 OverlapStart,
    UGPoint2 OverlapEnd,
    double A_T,
    double B_T,
    double A_T0,
    double A_T1)
{
    public bool IsEmpty => Kind == UGSegmentIntersectionKind.None;

    public static UGSegmentIntersection2 None =>
        new(UGSegmentIntersectionKind.None, UGPoint2.Empty, UGPoint2.Empty, UGPoint2.Empty, double.NaN, double.NaN, double.NaN, double.NaN);

    public static UGSegmentIntersection2 AtPoint(UGPoint2 point, double aT, double bT) =>
        new(UGSegmentIntersectionKind.Point, point, UGPoint2.Empty, UGPoint2.Empty, aT, bT, double.NaN, double.NaN);

    public static UGSegmentIntersection2 OverlapSegment(UGPoint2 start, UGPoint2 end, double aT0, double aT1) =>
        new(UGSegmentIntersectionKind.Overlap, UGPoint2.Empty, start, end, double.NaN, double.NaN, aT0, aT1);
}
