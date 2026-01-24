using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonNormalize
{
    public static UGPolygon2 EnsureCounterClockwise(UGPolygon2 polygon, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (polygon.Count < 3)
            return UGPolygon2.Empty;

        var simplified = PolygonSimplify.RemoveCollinearAndDuplicates(polygon, epsilon);
        if (simplified.IsEmpty)
            return UGPolygon2.Empty;

        var area = Polygon2.SignedArea(simplified);
        if (double.IsNaN(area) || Math.Abs(area) <= epsilon)
            return UGPolygon2.Empty;

        if (area > 0d)
            return simplified;

        var reversed = new UGPoint2[simplified.Count];
        for (var i = 0; i < simplified.Count; i++)
            reversed[i] = simplified[simplified.Count - 1 - i];

        return new UGPolygon2(reversed);
    }

    public static UGPolygon2 EnsureClockwise(UGPolygon2 polygon, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (polygon.Count < 3)
            return UGPolygon2.Empty;

        var simplified = PolygonSimplify.RemoveCollinearAndDuplicates(polygon, epsilon);
        if (simplified.IsEmpty)
            return UGPolygon2.Empty;

        var area = Polygon2.SignedArea(simplified);
        if (double.IsNaN(area) || Math.Abs(area) <= epsilon)
            return UGPolygon2.Empty;

        if (area < 0d)
            return simplified;

        var reversed = new UGPoint2[simplified.Count];
        for (var i = 0; i < simplified.Count; i++)
            reversed[i] = simplified[simplified.Count - 1 - i];

        return new UGPolygon2(reversed);
    }
}
