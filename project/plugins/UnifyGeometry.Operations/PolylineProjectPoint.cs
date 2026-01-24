using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolylineProjectPoint
{
    public static UGPoint2 ClosestPoint(UGPolyline2 polyline, UGPoint2 point)
        => ProjectPoint(polyline, point).Point;

    public static UGPolylineProjectedPoint2 ProjectPoint(UGPolyline2 polyline, UGPoint2 point)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (point.IsEmpty)
            return UGPolylineProjectedPoint2.Empty;

        if (polyline.IsEmpty)
            return UGPolylineProjectedPoint2.Empty;

        if (polyline.Count == 1)
        {
            var d2 = DistanceSquared(point, polyline[0]);
            return new UGPolylineProjectedPoint2(
                Point: polyline[0],
                SegmentIndex: 0,
                SegmentT: 0d,
                DistanceAlong: 0d,
                DistanceSquared: d2,
                SideSign: 0);
        }

        // Avoid undefined behavior in downstream math.
        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i].IsEmpty)
                return UGPolylineProjectedPoint2.Empty;
        }

        var bestD2 = double.PositiveInfinity;
        var bestPoint = UGPoint2.Empty;
        var bestSegIndex = -1;
        var bestT = double.NaN;
        var bestAlong = double.NaN;
        var bestSide = 0;

        var cum = 0d;

        for (var i = 0; i < polyline.Count - 1; i++)
        {
            var a = polyline[i];
            var b = polyline[i + 1];

            var segLen = a.DistanceTo(b);
            if (double.IsNaN(segLen) || double.IsInfinity(segLen))
                return UGPolylineProjectedPoint2.Empty;

            var proj = ProjectToSegment(point, a, b);

            if (proj.DistanceSquared < bestD2)
            {
                bestD2 = proj.DistanceSquared;
                bestPoint = proj.Point;
                bestSegIndex = i;
                bestT = proj.T;
                bestAlong = cum + (segLen * proj.T);
                bestSide = SideSign(point, a, b);
            }

            cum += segLen;
        }

        if (bestSegIndex < 0 || bestPoint.IsEmpty || double.IsInfinity(bestD2) || double.IsNaN(bestD2))
            return UGPolylineProjectedPoint2.Empty;

        return new UGPolylineProjectedPoint2(
            Point: bestPoint,
            SegmentIndex: bestSegIndex,
            SegmentT: bestT,
            DistanceAlong: bestAlong,
            DistanceSquared: bestD2,
            SideSign: bestSide);
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

    private static int SideSign(UGPoint2 p, UGPoint2 a, UGPoint2 b)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var apx = p.X - a.X;
        var apy = p.Y - a.Y;

        var cross = (abx * apy) - (aby * apx);
        if (double.IsNaN(cross) || double.IsInfinity(cross) || cross == 0d)
            return 0;

        return cross > 0d ? 1 : -1;
    }

    private static double DistanceSquared(UGPoint2 a, UGPoint2 b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return (dx * dx) + (dy * dy);
    }

    private readonly record struct SegmentProjection(UGPoint2 Point, double T, double DistanceSquared);
}

public readonly record struct UGPolylineProjectedPoint2(
    UGPoint2 Point,
    int SegmentIndex,
    double SegmentT,
    double DistanceAlong,
    double DistanceSquared,
    int SideSign)
{
    public bool IsEmpty => SegmentIndex < 0 || Point.IsEmpty;

    public static UGPolylineProjectedPoint2 Empty =>
        new(UGPoint2.Empty, -1, double.NaN, double.NaN, double.NaN, 0);
}
