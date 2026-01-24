using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonCentroid
{
    /// <summary>
    /// Returns the area-weighted centroid of a simple polygon.
    /// Returns <see cref="UGPoint2.Empty"/> for degenerate polygons (near-zero area) or invalid inputs.
    /// </summary>
    public static UGPoint2 OfPolygon(UGPolygon2 polygon, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (polygon.Count < 3)
            return UGPoint2.Empty;

        double cx = 0d;
        double cy = 0d;
        double twiceArea = 0d;

        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];

            if (a.IsEmpty || b.IsEmpty)
                return UGPoint2.Empty;

            var cross = (a.X * b.Y) - (b.X * a.Y);
            if (double.IsNaN(cross) || double.IsInfinity(cross))
                return UGPoint2.Empty;

            twiceArea += cross;
            cx += (a.X + b.X) * cross;
            cy += (a.Y + b.Y) * cross;
        }

        if (double.IsNaN(twiceArea) || double.IsInfinity(twiceArea))
            return UGPoint2.Empty;

        if (Math.Abs(twiceArea) <= epsilon)
            return UGPoint2.Empty;

        var inv = 1d / (3d * twiceArea);
        return new UGPoint2(cx * inv, cy * inv);
    }
}
