using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolylineClipHalfPlane
{
    /// <summary>
    /// Clips an open polyline to the half-plane defined by (p - pointOnBoundary)·inwardNormal >= 0.
    /// </summary>
    public static IReadOnlyList<UGPolyline2> ToHalfPlane(UGPolyline2 polyline, UGPoint2 pointOnBoundary, UGPoint2 inwardNormal)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (pointOnBoundary.IsEmpty || inwardNormal.IsEmpty)
            return Array.Empty<UGPolyline2>();

        var nx = inwardNormal.X;
        var ny = inwardNormal.Y;
        if (double.IsNaN(nx) || double.IsNaN(ny) || double.IsInfinity(nx) || double.IsInfinity(ny))
            throw new ArgumentOutOfRangeException(nameof(inwardNormal), "Normal must be finite.");

        if (Math.Abs(nx) <= 0d && Math.Abs(ny) <= 0d)
            throw new ArgumentOutOfRangeException(nameof(inwardNormal), "Normal must not be zero.");

        if (polyline.IsEmpty)
            return Array.Empty<UGPolyline2>();

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

            var fa = Eval(a);
            var fb = Eval(b);

            var aIn = fa >= 0d;
            var bIn = fb >= 0d;

            if (aIn && bIn)
            {
                EnsureCurrent();
                AddPointDedup(current!, a);
                AddPointDedup(current!, b);
                continue;
            }

            if (!aIn && !bIn)
            {
                FlushCurrent();
                continue;
            }

            var denom = fa - fb;
            if (Math.Abs(denom) < 1e-15)
            {
                // Extremely close / numerical edge: treat as outside transition.
                FlushCurrent();
                continue;
            }

            var t = fa / denom; // intersection on segment a->b
            if (t < 0d) t = 0d;
            if (t > 1d) t = 1d;

            var ix = a.X + ((b.X - a.X) * t);
            var iy = a.Y + ((b.Y - a.Y) * t);
            var inter = new UGPoint2(ix, iy);

            if (aIn && !bIn)
            {
                EnsureCurrent();
                AddPointDedup(current!, a);
                AddPointDedup(current!, inter);
                FlushCurrent();
            }
            else if (!aIn && bIn)
            {
                EnsureCurrent();
                AddPointDedup(current!, inter);
                AddPointDedup(current!, b);
            }
        }

        FlushCurrent();
        return results;

        double Eval(UGPoint2 p)
        {
            var dx = p.X - pointOnBoundary.X;
            var dy = p.Y - pointOnBoundary.Y;
            return (dx * nx) + (dy * ny);
        }

        void EnsureCurrent()
        {
            current ??= new List<UGPoint2>(4);
        }

        void FlushCurrent()
        {
            if (current == null)
                return;

            if (current.Count > 0)
                results.Add(new UGPolyline2(current));

            current = null;
        }
    }

    private static void AddPointDedup(List<UGPoint2> points, UGPoint2 p)
    {
        if (points.Count == 0 || !ApproximatelyEqual(points[^1], p))
            points.Add(p);
    }

    private static bool ApproximatelyEqual(UGPoint2 a, UGPoint2 b)
        => Math.Abs(a.X - b.X) <= 1e-12 && Math.Abs(a.Y - b.Y) <= 1e-12;
}
