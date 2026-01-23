using UnifyGeometry;

namespace UnifyGeometry.Operators;

public static class PolylineDensify
{
    public static UGPolyline2 ByMaxSegmentLength(UGPolyline2 polyline, double maxSegmentLength)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (double.IsNaN(maxSegmentLength) || double.IsInfinity(maxSegmentLength) || maxSegmentLength <= 0d)
            throw new ArgumentOutOfRangeException(nameof(maxSegmentLength), "maxSegmentLength must be finite and > 0.");

        if (polyline.Count < 2)
            return polyline;

        var outPoints = new List<UGPoint2>(polyline.Count);

        for (var i = 0; i < polyline.Count - 1; i++)
        {
            var a = polyline[i];
            var b = polyline[i + 1];

            if (i == 0)
                outPoints.Add(a);

            var segLen = a.DistanceTo(b);
            if (double.IsNaN(segLen) || segLen <= 0d)
            {
                outPoints.Add(b);
                continue;
            }

            var steps = (int)Math.Ceiling(segLen / maxSegmentLength);
            if (steps < 1)
                steps = 1;

            for (var s = 1; s < steps; s++)
            {
                var t = (double)s / steps;
                outPoints.Add(Lerp(a, b, t));
            }

            outPoints.Add(b);
        }

        return new UGPolyline2(outPoints);
    }

    private static UGPoint2 Lerp(UGPoint2 a, UGPoint2 b, double t)
        => new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));
}
