using UnifyGeometry;

namespace UnifyGeometry.Operators;

public static class PolylineClip
{
    /// <summary>
    /// Clips an open polyline against an axis-aligned rectangle (bounds) and returns the
    /// resulting inside pieces. Output may contain 0, 1, or multiple polylines.
    /// </summary>
    public static IReadOnlyList<UGPolyline2> ToBounds(UGPolyline2 polyline, UGBounds2 bounds)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (bounds.IsEmpty || polyline.IsEmpty)
            return Array.Empty<UGPolyline2>();

        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i].IsEmpty)
                return Array.Empty<UGPolyline2>();
        }

        var minX = Math.Min(bounds.Min.X, bounds.Max.X);
        var minY = Math.Min(bounds.Min.Y, bounds.Max.Y);
        var maxX = Math.Max(bounds.Min.X, bounds.Max.X);
        var maxY = Math.Max(bounds.Min.Y, bounds.Max.Y);

        var results = new List<UGPolyline2>();
        List<UGPoint2>? current = null;

        for (var i = 0; i < polyline.Count - 1; i++)
        {
            var a = polyline[i];
            var b = polyline[i + 1];

            if (!TryClipSegmentLiangBarsky(a, b, minX, minY, maxX, maxY, out var c0, out var c1))
            {
                FlushCurrent();
                continue;
            }

            if (current == null)
            {
                current = new List<UGPoint2>(4);
                AddPointDedup(current, c0);
                AddPointDedup(current, c1);
            }
            else
            {
                var last = current[^1];
                if (!ApproximatelyEqual(last, c0))
                {
                    FlushCurrent();
                    current = new List<UGPoint2>(4);
                    AddPointDedup(current, c0);
                    AddPointDedup(current, c1);
                }
                else
                {
                    AddPointDedup(current, c1);
                }
            }
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

    private static bool TryClipSegmentLiangBarsky(
        UGPoint2 a,
        UGPoint2 b,
        double minX,
        double minY,
        double maxX,
        double maxY,
        out UGPoint2 c0,
        out UGPoint2 c1)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;

        var t0 = 0d;
        var t1 = 1d;

        if (!ClipTest(-dx, a.X - minX, ref t0, ref t1) ||
            !ClipTest(dx, maxX - a.X, ref t0, ref t1) ||
            !ClipTest(-dy, a.Y - minY, ref t0, ref t1) ||
            !ClipTest(dy, maxY - a.Y, ref t0, ref t1))
        {
            c0 = default;
            c1 = default;
            return false;
        }

        c0 = new UGPoint2(a.X + (dx * t0), a.Y + (dy * t0));
        c1 = new UGPoint2(a.X + (dx * t1), a.Y + (dy * t1));
        return true;
    }

    private static bool ClipTest(double p, double q, ref double t0, ref double t1)
    {
        if (Math.Abs(p) < 1e-15)
        {
            // Parallel to this boundary: accept only if inside.
            return q >= 0d;
        }

        var r = q / p;
        if (p < 0d)
        {
            if (r > t1) return false;
            if (r > t0) t0 = r;
        }
        else
        {
            if (r < t0) return false;
            if (r < t1) t1 = r;
        }

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
