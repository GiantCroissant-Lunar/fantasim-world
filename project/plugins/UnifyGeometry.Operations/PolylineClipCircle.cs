using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolylineClipCircle
{
    public static IReadOnlyList<UGPolyline2> ToCircle(UGPolyline2 polyline, UGPoint2 center, double radius)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (center.IsEmpty || double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0d)
            throw new ArgumentOutOfRangeException(nameof(radius), "radius must be finite and >= 0, and center must not be empty.");

        if (polyline.IsEmpty)
            return Array.Empty<UGPolyline2>();

        if (radius == 0d)
            return Array.Empty<UGPolyline2>();

        return ClipByRadialPredicate(polyline, center, t => t <= (radius * radius), radii: new[] { radius });
    }

    public static IReadOnlyList<UGPolyline2> ToAnnulus(UGPolyline2 polyline, UGPoint2 center, double innerRadius, double outerRadius)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (center.IsEmpty ||
            double.IsNaN(innerRadius) || double.IsInfinity(innerRadius) ||
            double.IsNaN(outerRadius) || double.IsInfinity(outerRadius))
        {
            throw new ArgumentOutOfRangeException(nameof(innerRadius), "Radii must be finite and center must not be empty.");
        }

        if (innerRadius < 0d || outerRadius < 0d)
            throw new ArgumentOutOfRangeException(nameof(innerRadius), "Radii must be >= 0.");

        var r0 = Math.Min(innerRadius, outerRadius);
        var r1 = Math.Max(innerRadius, outerRadius);

        if (polyline.IsEmpty)
            return Array.Empty<UGPolyline2>();

        if (r1 == 0d)
            return Array.Empty<UGPolyline2>();

        var r0Sq = r0 * r0;
        var r1Sq = r1 * r1;

        // Keep points with r0^2 <= dist^2 <= r1^2.
        return ClipByRadialPredicate(polyline, center, d2 => d2 >= r0Sq && d2 <= r1Sq, radii: r0 == 0d ? new[] { r1 } : new[] { r0, r1 });
    }

    private static IReadOnlyList<UGPolyline2> ClipByRadialPredicate(
        UGPolyline2 polyline,
        UGPoint2 center,
        Func<double, bool> predicate,
        double[] radii)
    {
        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i].IsEmpty)
                return Array.Empty<UGPolyline2>();
        }

        var results = new List<UGPolyline2>();
        List<UGPoint2>? current = null;

        for (var i = 0; i < polyline.Count - 1; i++)
        {
            var a = polyline[i];
            var b = polyline[i + 1];

            var ts = new List<double>(2 + (radii.Length * 2))
            {
                0d,
                1d
            };

            foreach (var r in radii)
            {
                if (TryCircleIntersectionParameters(a, b, center, r, out var tA, out var tB, out var count))
                {
                    if (count >= 1) AddT(ts, tA);
                    if (count >= 2) AddT(ts, tB);
                }
            }

            ts.Sort();
            UniqueInPlace(ts);

            var emittedAny = false;

            for (var j = 0; j < ts.Count - 1; j++)
            {
                var t0 = ts[j];
                var t1 = ts[j + 1];
                if (t1 - t0 <= 1e-15)
                    continue;

                var mid = (t0 + t1) * 0.5;
                var midPt = Lerp(a, b, mid);
                var d2 = DistSq(midPt, center);

                if (!predicate(d2))
                    continue;

                var p0 = Lerp(a, b, t0);
                var p1 = Lerp(a, b, t1);

                if (current == null)
                {
                    current = new List<UGPoint2>(4);
                    AddPointDedup(current, p0);
                    AddPointDedup(current, p1);
                }
                else
                {
                    var last = current[^1];
                    if (!ApproximatelyEqual(last, p0))
                    {
                        FlushCurrent();
                        current = new List<UGPoint2>(4);
                        AddPointDedup(current, p0);
                        AddPointDedup(current, p1);
                    }
                    else
                    {
                        AddPointDedup(current, p1);
                    }
                }

                emittedAny = true;
            }

            if (!emittedAny)
                FlushCurrent();
        }

        FlushCurrent();
        return results;

        void FlushCurrent()
        {
            if (current == null)
                return;

            if (current.Count > 0)
                results.Add(new UGPolyline2(current));

            current = null;
        }
    }

    private static bool TryCircleIntersectionParameters(
        UGPoint2 a,
        UGPoint2 b,
        UGPoint2 c,
        double r,
        out double tA,
        out double tB,
        out int count)
    {
        // Solve |(a + t d) - c|^2 = r^2.
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;

        var fx = a.X - c.X;
        var fy = a.Y - c.Y;

        var A = (dx * dx) + (dy * dy);
        if (A <= 0d)
        {
            tA = 0d;
            tB = 0d;
            count = 0;
            return false;
        }

        var B = 2d * ((fx * dx) + (fy * dy));
        var C = (fx * fx) + (fy * fy) - (r * r);

        var disc = (B * B) - (4d * A * C);
        if (disc < 0d)
        {
            tA = 0d;
            tB = 0d;
            count = 0;
            return false;
        }

        var sqrt = Math.Sqrt(Math.Max(0d, disc));
        var inv = 1d / (2d * A);

        tA = (-B - sqrt) * inv;
        tB = (-B + sqrt) * inv;
        count = ApproximatelyEqualDouble(tA, tB) ? 1 : 2;
        return true;
    }

    private static void AddT(List<double> ts, double t)
    {
        if (t <= 0d) { ts.Add(0d); return; }
        if (t >= 1d) { ts.Add(1d); return; }
        ts.Add(t);
    }

    private static void UniqueInPlace(List<double> ts)
    {
        if (ts.Count <= 1)
            return;

        var write = 1;
        var last = ts[0];
        for (var i = 1; i < ts.Count; i++)
        {
            var v = ts[i];
            if (Math.Abs(v - last) <= 1e-12)
                continue;

            ts[write++] = v;
            last = v;
        }

        if (write < ts.Count)
            ts.RemoveRange(write, ts.Count - write);
    }

    private static UGPoint2 Lerp(UGPoint2 a, UGPoint2 b, double t)
        => new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));

    private static double DistSq(UGPoint2 a, UGPoint2 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return (dx * dx) + (dy * dy);
    }

    private static void AddPointDedup(List<UGPoint2> points, UGPoint2 p)
    {
        if (points.Count == 0 || !ApproximatelyEqual(points[^1], p))
            points.Add(p);
    }

    private static bool ApproximatelyEqual(UGPoint2 a, UGPoint2 b)
        => Math.Abs(a.X - b.X) <= 1e-12 && Math.Abs(a.Y - b.Y) <= 1e-12;

    private static bool ApproximatelyEqualDouble(double a, double b)
        => Math.Abs(a - b) <= 1e-12;
}
