using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolylineArcLength
{
    public static UGPoint2 PointAtDistance(UGPolyline2 polyline, double distance)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (double.IsNaN(distance))
            return UGPoint2.Empty;

        if (polyline.IsEmpty)
            return UGPoint2.Empty;

        if (polyline.Count == 1)
            return polyline[0];

        // Avoid undefined behavior in downstream math.
        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i].IsEmpty)
                return UGPoint2.Empty;
        }

        var cum = CumulativeLengths(polyline);
        var total = cum[^1];
        if (double.IsNaN(total) || double.IsInfinity(total))
            return UGPoint2.Empty;

        if (distance <= 0d || total <= 0d)
            return polyline[0];

        if (distance >= total)
            return polyline[^1];

        var location = Locate(polyline, cum, distance);
        return location.Point;
    }

    public static UGPolyline2 SliceByDistance(UGPolyline2 polyline, double startDistance, double endDistance)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (double.IsNaN(startDistance) || double.IsNaN(endDistance))
            return polyline;

        if (polyline.IsEmpty)
            return UGPolyline2.Empty;

        if (polyline.Count == 1)
        {
            // Degenerate polyline: treat any positive-length interval overlapping 0 as containing the point.
            if (startDistance < endDistance && startDistance <= 0d && endDistance > 0d)
                return polyline;

            return UGPolyline2.Empty;
        }

        // Avoid undefined behavior in downstream math.
        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i].IsEmpty)
                return polyline;
        }

        var cum = CumulativeLengths(polyline);
        var total = cum[^1];
        if (double.IsNaN(total) || double.IsInfinity(total))
            return polyline;

        return SliceByDistanceCore(polyline, cum, total, startDistance, endDistance);
    }

    public static IReadOnlyList<UGPolyline2> SplitByDistances(UGPolyline2 polyline, IEnumerable<double> distances)
    {
        ArgumentNullException.ThrowIfNull(polyline);
        ArgumentNullException.ThrowIfNull(distances);

        if (polyline.IsEmpty)
            return Array.Empty<UGPolyline2>();

        if (polyline.Count == 1)
            return new[] { polyline };

        // Avoid undefined behavior in downstream math.
        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i].IsEmpty)
                return new[] { polyline };
        }

        var cum = CumulativeLengths(polyline);
        var total = cum[^1];
        if (double.IsNaN(total) || double.IsInfinity(total))
            return new[] { polyline };

        if (total <= 0d)
            return new[] { polyline };

        var cuts = new List<double>();
        foreach (var d in distances)
        {
            if (double.IsNaN(d) || double.IsInfinity(d))
                continue;

            cuts.Add(Clamp(d, 0d, total));
        }

        if (cuts.Count == 0)
            return new[] { polyline };

        cuts.Sort();

        var breakpoints = new List<double>(capacity: cuts.Count + 2) { 0d };
        var last = 0d;
        foreach (var d in cuts)
        {
            if (d <= last || d <= 0d || d >= total)
                continue;

            breakpoints.Add(d);
            last = d;
        }

        if (breakpoints.Count == 1)
            return new[] { polyline };

        breakpoints.Add(total);

        var pieces = new List<UGPolyline2>(capacity: breakpoints.Count - 1);
        for (var i = 0; i < breakpoints.Count - 1; i++)
        {
            var a = breakpoints[i];
            var b = breakpoints[i + 1];
            if (b <= a)
                continue;

            var slice = SliceByDistanceCore(polyline, cum, total, a, b);
            if (!slice.IsEmpty)
                pieces.Add(slice);
        }

        return pieces;
    }

    private static double Clamp(double v, double min, double max)
        => v < min ? min : (v > max ? max : v);

    private static UGPolyline2 SliceByDistanceCore(
        UGPolyline2 polyline,
        double[] cum,
        double total,
        double startDistance,
        double endDistance)
    {
        if (total <= 0d)
        {
            if (startDistance < endDistance && startDistance <= 0d && endDistance > 0d)
                return polyline;

            return UGPolyline2.Empty;
        }

        var a = Clamp(startDistance, 0d, total);
        var b = Clamp(endDistance, 0d, total);
        if (b <= a)
            return UGPolyline2.Empty;

        var start = Locate(polyline, cum, a);
        var end = Locate(polyline, cum, b);

        var outPoints = new List<UGPoint2>();
        outPoints.Add(start.Point);

        var firstInteriorVertexIndex = start.VertexIndex is not null ? start.VertexIndex.Value + 1 : start.SegmentIndex + 1;
        var lastExclusive = end.VertexIndex is not null ? end.VertexIndex.Value : end.SegmentIndex + 1;

        if (firstInteriorVertexIndex < 0)
            firstInteriorVertexIndex = 0;

        if (lastExclusive > polyline.Count)
            lastExclusive = polyline.Count;

        for (var i = firstInteriorVertexIndex; i < lastExclusive; i++)
        {
            outPoints.Add(polyline[i]);
        }

        if (outPoints.Count == 0 || outPoints[^1] != end.Point)
            outPoints.Add(end.Point);

        return new UGPolyline2(outPoints);
    }

    private static double[] CumulativeLengths(UGPolyline2 polyline)
    {
        var cum = new double[polyline.Count];
        cum[0] = 0d;
        for (var i = 1; i < polyline.Count; i++)
        {
            var seg = new UGSegment2(polyline[i - 1], polyline[i]);
            cum[i] = cum[i - 1] + seg.Length;
        }

        return cum;
    }

    private static LocatedPoint Locate(UGPolyline2 polyline, double[] cum, double distance)
    {
        if (distance <= 0d)
            return new LocatedPoint(polyline[0], SegmentIndex: 0, VertexIndex: 0);

        var total = cum[^1];
        if (distance >= total)
            return new LocatedPoint(polyline[^1], SegmentIndex: polyline.Count - 2, VertexIndex: polyline.Count - 1);

        var segIndex = 0;
        while (segIndex < cum.Length - 2 && cum[segIndex + 1] < distance)
        {
            segIndex++;
        }

        var a = polyline[segIndex];
        var b = polyline[segIndex + 1];
        var aDist = cum[segIndex];
        var bDist = cum[segIndex + 1];

        var denom = bDist - aDist;
        if (denom <= 0d)
            return new LocatedPoint(a, SegmentIndex: segIndex, VertexIndex: segIndex);

        var t = (distance - aDist) / denom;

        if (t <= 0d)
            return new LocatedPoint(a, SegmentIndex: segIndex, VertexIndex: segIndex);

        if (t >= 1d)
            return new LocatedPoint(b, SegmentIndex: segIndex, VertexIndex: segIndex + 1);

        var p = Lerp(a, b, t);
        return new LocatedPoint(p, SegmentIndex: segIndex, VertexIndex: null);
    }

    private static UGPoint2 Lerp(UGPoint2 a, UGPoint2 b, double t)
        => new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));

    private readonly record struct LocatedPoint(UGPoint2 Point, int SegmentIndex, int? VertexIndex);
}
