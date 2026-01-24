using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonClip
{
    public static UGPolygon2 ToBounds(UGPolygon2 polygon, UGBounds2 bounds)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (bounds.IsEmpty)
            return UGPolygon2.Empty;

        if (polygon.Count < 3)
            return UGPolygon2.Empty;

        // Clip in chart space:
        // x >= minX, x <= maxX, y >= minY, y <= maxY
        var p = polygon;

        // x >= minX
        p = PolygonClipHalfPlane.ToHalfPlane(p, new UGPoint2(bounds.Min.X, 0), new UGPoint2(1, 0));
        if (p.IsEmpty) return p;

        // x <= maxX  => (p - (maxX,0))·(-1,0) >= 0
        p = PolygonClipHalfPlane.ToHalfPlane(p, new UGPoint2(bounds.Max.X, 0), new UGPoint2(-1, 0));
        if (p.IsEmpty) return p;

        // y >= minY
        p = PolygonClipHalfPlane.ToHalfPlane(p, new UGPoint2(0, bounds.Min.Y), new UGPoint2(0, 1));
        if (p.IsEmpty) return p;

        // y <= maxY
        p = PolygonClipHalfPlane.ToHalfPlane(p, new UGPoint2(0, bounds.Max.Y), new UGPoint2(0, -1));
        return p;
    }
}
