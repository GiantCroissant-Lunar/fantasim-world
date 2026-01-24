using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonRegion2
{
    public static bool ContainsPoint(UGPolygonRegion2 region, UGPoint2 point, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(region);

        if (point.IsEmpty)
            return false;

        if (region.IsEmpty)
            return false;

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (!Polygon2.ContainsPoint(region.Outer, point, epsilon))
            return false;

        for (var i = 0; i < region.Holes.Count; i++)
        {
            if (Polygon2.ContainsPoint(region.Holes[i], point, epsilon))
                return false;
        }

        return true;
    }

    public static double Area(UGPolygonRegion2 region)
    {
        ArgumentNullException.ThrowIfNull(region);

        if (region.IsEmpty)
            return 0d;

        var a = Math.Abs(Polygon2.SignedArea(region.Outer));
        if (double.IsNaN(a) || double.IsInfinity(a))
            return double.NaN;

        for (var i = 0; i < region.Holes.Count; i++)
        {
            var h = Math.Abs(Polygon2.SignedArea(region.Holes[i]));
            if (double.IsNaN(h) || double.IsInfinity(h))
                return double.NaN;
            a -= h;
        }

        return a < 0d ? 0d : a;
    }
}
