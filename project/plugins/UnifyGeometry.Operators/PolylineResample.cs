using UnifyGeometry;

namespace UnifyGeometry.Operators;

public static class PolylineResample
{
    public static UGPolyline2 ByPointCount(UGPolyline2 polyline, int pointCount)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (pointCount < 0)
            throw new ArgumentOutOfRangeException(nameof(pointCount), "pointCount must be >= 0.");

        if (pointCount == 0 || polyline.IsEmpty)
            return UGPolyline2.Empty;

        if (pointCount == 1)
            return new UGPolyline2(new[] { polyline[0] });

        if (polyline.Count == 1)
        {
            var repeated = new UGPoint2[pointCount];
            Array.Fill(repeated, polyline[0]);
            return new UGPolyline2(repeated);
        }

        // Avoid undefined behavior in downstream math.
        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i].IsEmpty)
                return polyline;
        }

        // Cumulative lengths (monotone non-decreasing).
        var cum = new double[polyline.Count];
        cum[0] = 0d;
        for (var i = 1; i < polyline.Count; i++)
        {
            var seg = new UGSegment2(polyline[i - 1], polyline[i]);
            cum[i] = cum[i - 1] + seg.Length;
        }

        var total = cum[^1];
        if (double.IsNaN(total) || double.IsInfinity(total))
            return polyline;

        if (total <= 0d)
        {
            var repeated = new UGPoint2[pointCount];
            Array.Fill(repeated, polyline[0]);
            return new UGPolyline2(repeated);
        }

        var step = total / (pointCount - 1);
        var outPoints = new UGPoint2[pointCount];
        outPoints[0] = polyline[0];
        outPoints[^1] = polyline[^1];

        var segIndex = 0;
        for (var k = 1; k < pointCount - 1; k++)
        {
            var target = step * k;

            while (segIndex < cum.Length - 2 && cum[segIndex + 1] < target)
            {
                segIndex++;
            }

            var a = polyline[segIndex];
            var b = polyline[segIndex + 1];
            var aDist = cum[segIndex];
            var bDist = cum[segIndex + 1];

            var denom = bDist - aDist;
            if (denom <= 0d)
            {
                outPoints[k] = a;
                continue;
            }

            var t = (target - aDist) / denom;
            outPoints[k] = Lerp(a, b, t);
        }

        return new UGPolyline2(outPoints);
    }

    private static UGPoint2 Lerp(UGPoint2 a, UGPoint2 b, double t)
        => new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));
}
