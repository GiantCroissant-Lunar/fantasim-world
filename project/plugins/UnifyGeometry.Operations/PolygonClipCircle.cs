using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonClipCircle
{
    public static UGPolygon2 ToCircle(UGPolygon2 polygon, UGPoint2 center, double radius, int segmentCount = 128)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (center.IsEmpty)
            return UGPolygon2.Empty;

        if (double.IsNaN(radius) || double.IsInfinity(radius) || radius <= 0d)
            throw new ArgumentOutOfRangeException(nameof(radius), "radius must be finite and > 0.");

        if (segmentCount < 3)
            throw new ArgumentOutOfRangeException(nameof(segmentCount), "segmentCount must be >= 3.");

        var circle = RegularPolygon(center, radius, segmentCount);
        var clipped = PolygonClipConvex.ToConvexPolygon(polygon, circle);
        return clipped.IsEmpty ? UGPolygon2.Empty : PolygonSimplify.RemoveCollinearAndDuplicates(clipped);
    }

    public static UGPolygonRegion2 ToAnnulus(
        UGPolygon2 polygon,
        UGPoint2 center,
        double innerRadius,
        double outerRadius,
        int segmentCount = 128)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (center.IsEmpty)
            return UGPolygonRegion2.Empty;

        if (double.IsNaN(innerRadius) || double.IsInfinity(innerRadius) || innerRadius < 0d)
            throw new ArgumentOutOfRangeException(nameof(innerRadius), "innerRadius must be finite and >= 0.");

        if (double.IsNaN(outerRadius) || double.IsInfinity(outerRadius) || outerRadius <= 0d)
            throw new ArgumentOutOfRangeException(nameof(outerRadius), "outerRadius must be finite and > 0.");

        if (outerRadius <= innerRadius)
            throw new ArgumentOutOfRangeException(nameof(outerRadius), "outerRadius must be > innerRadius.");

        if (segmentCount < 3)
            throw new ArgumentOutOfRangeException(nameof(segmentCount), "segmentCount must be >= 3.");

        var outer = ToCircle(polygon, center, outerRadius, segmentCount);
        if (outer.IsEmpty)
            return UGPolygonRegion2.Empty;

        if (innerRadius <= 0d)
            return new UGPolygonRegion2(outer, Array.Empty<UGPolygon2>());

        var inner = ToCircle(polygon, center, innerRadius, segmentCount);
        if (inner.IsEmpty)
            return new UGPolygonRegion2(outer, Array.Empty<UGPolygon2>());

        // Best-effort: include a hole only if it is contained by the outer ring.
        var holeProbe = PolygonCentroid.OfPolygon(inner);
        if (!holeProbe.IsEmpty && Polygon2.ContainsPoint(outer, holeProbe))
            return new UGPolygonRegion2(outer, new[] { inner });

        return new UGPolygonRegion2(outer, Array.Empty<UGPolygon2>());
    }

    private static UGPolygon2 RegularPolygon(UGPoint2 center, double radius, int segmentCount)
    {
        var pts = new UGPoint2[segmentCount];
        var step = (Math.PI * 2d) / segmentCount;

        for (var i = 0; i < segmentCount; i++)
        {
            var a = step * i;
            var x = center.X + (Math.Cos(a) * radius);
            var y = center.Y + (Math.Sin(a) * radius);
            pts[i] = new UGPoint2(x, y);
        }

        // CCW by construction (increasing angle).
        return new UGPolygon2(pts);
    }
}
