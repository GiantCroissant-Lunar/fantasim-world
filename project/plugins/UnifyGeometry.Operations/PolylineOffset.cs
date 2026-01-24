using UnifyGeometry;

namespace UnifyGeometry.Operations;

public enum PolylineOffsetSide
{
    Left = 0,
    Right = 1
}

public static class PolylineOffset
{
    /// <summary>
    /// Offsets an open polyline by a constant distance, producing a new polyline with the same point count.
    /// Join handling uses a miter join with a miter limit clamp.
    /// </summary>
    public static UGPolyline2 ByDistance(
        UGPolyline2 polyline,
        double distance,
        PolylineOffsetSide side = PolylineOffsetSide.Left,
        double miterLimit = 4.0)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (double.IsNaN(distance) || double.IsInfinity(distance))
            throw new ArgumentOutOfRangeException(nameof(distance), "distance must be finite.");

        if (double.IsNaN(miterLimit) || double.IsInfinity(miterLimit) || miterLimit <= 0d)
            throw new ArgumentOutOfRangeException(nameof(miterLimit), "miterLimit must be finite and > 0.");

        if (polyline.Count == 0 || distance == 0d)
            return polyline;

        if (polyline.Count == 1)
        {
            // Undefined normal; return as-is.
            return polyline;
        }

        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i].IsEmpty)
                return polyline;
        }

        var signed = side == PolylineOffsetSide.Left ? distance : -distance;

        var pts = new UGPoint2[polyline.Count];

        // Endpoints.
        pts[0] = OffsetEndpoint(polyline[0], polyline[1], signed);
        pts[^1] = OffsetEndpoint(polyline[^1], polyline[^2], -signed); // reverse direction for last segment normal

        for (var i = 1; i < polyline.Count - 1; i++)
        {
            var prev = polyline[i - 1];
            var cur = polyline[i];
            var next = polyline[i + 1];

            if (!TryUnitDirection(prev, cur, out var d0x, out var d0y) ||
                !TryUnitDirection(cur, next, out var d1x, out var d1y))
            {
                // Degenerate - fall back to whichever segment has a direction.
                if (TryUnitDirection(prev, cur, out d0x, out d0y))
                {
                    var n = Normal(d0x, d0y, signed);
                    pts[i] = new UGPoint2(cur.X + n.Nx, cur.Y + n.Ny);
                    continue;
                }
                if (TryUnitDirection(cur, next, out d1x, out d1y))
                {
                    var n = Normal(d1x, d1y, signed);
                    pts[i] = new UGPoint2(cur.X + n.Nx, cur.Y + n.Ny);
                    continue;
                }

                pts[i] = cur;
                continue;
            }

            var n0 = Normal(d0x, d0y, signed);
            var n1 = Normal(d1x, d1y, signed);

            // Offset lines pass through cur shifted by each segment normal.
            var p0 = new UGPoint2(cur.X + n0.Nx, cur.Y + n0.Ny);
            var p1 = new UGPoint2(cur.X + n1.Nx, cur.Y + n1.Ny);

            // Line directions are the segment directions.
            if (TryLineIntersection(p0, d0x, d0y, p1, d1x, d1y, out var ix, out var iy))
            {
                var miterX = ix - cur.X;
                var miterY = iy - cur.Y;
                var miterLen = Math.Sqrt((miterX * miterX) + (miterY * miterY));

                var maxMiter = Math.Abs(distance) * miterLimit;
                if (miterLen > maxMiter && miterLen > 0d)
                {
                    var scale = maxMiter / miterLen;
                    ix = cur.X + (miterX * scale);
                    iy = cur.Y + (miterY * scale);
                }

                pts[i] = new UGPoint2(ix, iy);
                continue;
            }

            // Parallel-ish: fall back to averaged normal (or single normal).
            var avgNx = n0.Nx + n1.Nx;
            var avgNy = n0.Ny + n1.Ny;
            var avgLen = Math.Sqrt((avgNx * avgNx) + (avgNy * avgNy));
            if (avgLen > 0d)
            {
                pts[i] = new UGPoint2(cur.X + (avgNx / avgLen) * Math.Abs(distance), cur.Y + (avgNy / avgLen) * Math.Abs(distance));
            }
            else
            {
                pts[i] = new UGPoint2(cur.X + n0.Nx, cur.Y + n0.Ny);
            }
        }

        return new UGPolyline2(pts);
    }

    private static UGPoint2 OffsetEndpoint(UGPoint2 a, UGPoint2 b, double signedDistance)
    {
        if (!TryUnitDirection(a, b, out var dx, out var dy))
            return a;

        var n = Normal(dx, dy, signedDistance);
        return new UGPoint2(a.X + n.Nx, a.Y + n.Ny);
    }

    private static bool TryUnitDirection(UGPoint2 a, UGPoint2 b, out double dx, out double dy)
    {
        dx = b.X - a.X;
        dy = b.Y - a.Y;
        var len = Math.Sqrt((dx * dx) + (dy * dy));
        if (len <= 0d || double.IsNaN(len) || double.IsInfinity(len))
        {
            dx = 0d;
            dy = 0d;
            return false;
        }

        dx /= len;
        dy /= len;
        return true;
    }

    private static (double Nx, double Ny) Normal(double unitDx, double unitDy, double signedDistance)
    {
        // Left normal of direction (dx,dy) is (-dy, dx).
        var nx = -unitDy;
        var ny = unitDx;
        return (nx * signedDistance, ny * signedDistance);
    }

    private static bool TryLineIntersection(
        UGPoint2 p0,
        double d0x,
        double d0y,
        UGPoint2 p1,
        double d1x,
        double d1y,
        out double ix,
        out double iy)
    {
        // Solve p0 + t*d0 = p1 + u*d1.
        // 2D: determinant = cross(d0, d1).
        var det = (d0x * d1y) - (d0y * d1x);
        if (Math.Abs(det) < 1e-12)
        {
            ix = 0d;
            iy = 0d;
            return false;
        }

        var px = p1.X - p0.X;
        var py = p1.Y - p0.Y;

        var t = ((px * d1y) - (py * d1x)) / det;
        ix = p0.X + (d0x * t);
        iy = p0.Y + (d0y * t);
        return true;
    }
}
