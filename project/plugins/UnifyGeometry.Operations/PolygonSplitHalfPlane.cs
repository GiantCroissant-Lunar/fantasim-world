using UnifyGeometry;

namespace UnifyGeometry.Operations;

public static class PolygonSplitHalfPlane
{
    /// <summary>
    /// Splits a polygon by a half-plane boundary line into up to two polygons:
    /// <list type="bullet">
    /// <item><description><see cref="UGPolygonSplit2.Inside"/> where (p - pointOnBoundary)·inwardNormal &gt;= 0</description></item>
    /// <item><description><see cref="UGPolygonSplit2.Outside"/> where (p - pointOnBoundary)·inwardNormal &lt;= 0 (computed via the opposite normal)</description></item>
    /// </list>
    /// Boundary points may appear in both outputs.
    /// </summary>
    public static UGPolygonSplit2 Split(UGPolygon2 polygon, UGPoint2 pointOnBoundary, UGPoint2 inwardNormal, double epsilon = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (double.IsNaN(epsilon) || double.IsInfinity(epsilon) || epsilon < 0d)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "epsilon must be finite and >= 0.");

        if (polygon.Count < 3)
            return UGPolygonSplit2.Empty;

        if (pointOnBoundary.IsEmpty || inwardNormal.IsEmpty)
            return UGPolygonSplit2.Empty;

        var nx = inwardNormal.X;
        var ny = inwardNormal.Y;
        if (double.IsNaN(nx) || double.IsNaN(ny) || double.IsInfinity(nx) || double.IsInfinity(ny))
            throw new ArgumentOutOfRangeException(nameof(inwardNormal), "Normal must be finite.");

        if (Math.Abs(nx) <= 0d && Math.Abs(ny) <= 0d)
            throw new ArgumentOutOfRangeException(nameof(inwardNormal), "Normal must not be zero.");

        var inside = PolygonClipHalfPlane.ToHalfPlane(polygon, pointOnBoundary, inwardNormal);
        inside = inside.IsEmpty ? UGPolygon2.Empty : PolygonSimplify.RemoveCollinearAndDuplicates(inside, epsilon);

        var outsideNormal = new UGPoint2(-inwardNormal.X, -inwardNormal.Y);
        var outside = PolygonClipHalfPlane.ToHalfPlane(polygon, pointOnBoundary, outsideNormal);
        outside = outside.IsEmpty ? UGPolygon2.Empty : PolygonSimplify.RemoveCollinearAndDuplicates(outside, epsilon);

        if (inside.IsEmpty && outside.IsEmpty)
            return UGPolygonSplit2.Empty;

        return new UGPolygonSplit2(inside, outside);
    }
}

public readonly record struct UGPolygonSplit2(UGPolygon2 Inside, UGPolygon2 Outside)
{
    public static UGPolygonSplit2 Empty => new(UGPolygon2.Empty, UGPolygon2.Empty);

    public bool IsEmpty => Inside.IsEmpty && Outside.IsEmpty;
}
