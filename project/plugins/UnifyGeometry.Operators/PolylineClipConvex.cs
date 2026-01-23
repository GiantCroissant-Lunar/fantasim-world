using UnifyGeometry;

namespace UnifyGeometry.Operators;

public static class PolylineClipConvex
{
    /// <summary>
    /// Clips an open polyline against a convex polygon and returns the resulting inside pieces.
    /// The polygon may be provided in CW or CCW order; empties/degenerate polygons return empty.
    /// </summary>
    public static IReadOnlyList<UGPolyline2> ToConvexPolygon(UGPolyline2 polyline, IReadOnlyList<UGPoint2> convexPolygon)
    {
        ArgumentNullException.ThrowIfNull(polyline);
        ArgumentNullException.ThrowIfNull(convexPolygon);

        if (polyline.IsEmpty || convexPolygon.Count < 3)
            return Array.Empty<UGPolyline2>();

        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i].IsEmpty)
                return Array.Empty<UGPolyline2>();
        }

        for (var i = 0; i < convexPolygon.Count; i++)
        {
            if (convexPolygon[i].IsEmpty)
                return Array.Empty<UGPolyline2>();
        }

        var isCcw = SignedArea(convexPolygon) >= 0d;

        var results = new List<UGPolyline2>();
        List<UGPoint2>? current = null;

        for (var i = 0; i < polyline.Count - 1; i++)
        {
            var a = polyline[i];
            var b = polyline[i + 1];

            if (!TryClipSegmentCyrusBeck(a, b, convexPolygon, isCcw, out var c0, out var c1))
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

    private static bool TryClipSegmentCyrusBeck(
        UGPoint2 a,
        UGPoint2 b,
        IReadOnlyList<UGPoint2> poly,
        bool isCcw,
        out UGPoint2 c0,
        out UGPoint2 c1)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;

        var t0 = 0d;
        var t1 = 1d;

        for (var i = 0; i < poly.Count; i++)
        {
            var p0 = poly[i];
            var p1 = poly[(i + 1) % poly.Count];

            var ex = p1.X - p0.X;
            var ey = p1.Y - p0.Y;

            // Inward normal depends on winding.
            // For CCW polygon, inward normal is left normal (-ey, ex).
            // For CW polygon, inward normal is right normal (ey, -ex).
            var nx = isCcw ? -ey : ey;
            var ny = isCcw ? ex : -ex;

            var denom = (nx * dx) + (ny * dy); // n·d
            var num = (nx * (p0.X - a.X)) + (ny * (p0.Y - a.Y)); // n·(p0 - a)

            if (Math.Abs(denom) < 1e-15)
            {
                if (num > 0d)
                {
                    c0 = default;
                    c1 = default;
                    return false;
                }

                continue;
            }

            var t = num / denom; // constraint: t*denom >= num
            if (denom > 0d)
            {
                if (t > t0) t0 = t;
            }
            else
            {
                if (t < t1) t1 = t;
            }

            if (t0 - t1 > 1e-15)
            {
                c0 = default;
                c1 = default;
                return false;
            }
        }

        c0 = new UGPoint2(a.X + (dx * t0), a.Y + (dy * t0));
        c1 = new UGPoint2(a.X + (dx * t1), a.Y + (dy * t1));
        return true;
    }

    private static double SignedArea(IReadOnlyList<UGPoint2> poly)
    {
        var sum = 0d;
        for (var i = 0; i < poly.Count; i++)
        {
            var a = poly[i];
            var b = poly[(i + 1) % poly.Count];
            sum += (a.X * b.Y) - (b.X * a.Y);
        }

        return 0.5 * sum;
    }

    private static void AddPointDedup(List<UGPoint2> points, UGPoint2 p)
    {
        if (points.Count == 0 || !ApproximatelyEqual(points[^1], p))
            points.Add(p);
    }

    private static bool ApproximatelyEqual(UGPoint2 a, UGPoint2 b)
        => Math.Abs(a.X - b.X) <= 1e-12 && Math.Abs(a.Y - b.Y) <= 1e-12;
}
