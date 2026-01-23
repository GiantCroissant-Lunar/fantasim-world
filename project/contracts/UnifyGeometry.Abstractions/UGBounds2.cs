namespace UnifyGeometry;

public readonly record struct UGBounds2(UGPoint2 Min, UGPoint2 Max)
{
    public bool IsEmpty => Min.IsEmpty || Max.IsEmpty;

    public static UGBounds2 Empty => new(UGPoint2.Empty, UGPoint2.Empty);

    public static UGBounds2 FromPoints(ReadOnlySpan<UGPoint2> points)
    {
        if (points.Length == 0)
            return Empty;

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;

        for (var i = 0; i < points.Length; i++)
        {
            var p = points[i];
            if (p.IsEmpty)
                continue;

            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }

        if (double.IsInfinity(minX) || double.IsInfinity(minY) || double.IsInfinity(maxX) || double.IsInfinity(maxY))
            return Empty;

        return new UGBounds2(new UGPoint2(minX, minY), new UGPoint2(maxX, maxY));
    }
}
