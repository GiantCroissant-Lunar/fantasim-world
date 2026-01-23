namespace UnifyGeometry;

public readonly record struct UGPoint2(double X, double Y)
{
    public bool IsEmpty => double.IsNaN(X) || double.IsNaN(Y);

    public static UGPoint2 Empty => new(double.NaN, double.NaN);

    public double DistanceTo(UGPoint2 other)
    {
        if (IsEmpty || other.IsEmpty)
            return double.NaN;

        var dx = other.X - X;
        var dy = other.Y - Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
