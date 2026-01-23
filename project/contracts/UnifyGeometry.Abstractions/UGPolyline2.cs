namespace UnifyGeometry;

public sealed class UGPolyline2
{
    private readonly UGPoint2[] _points;

    public UGPolyline2()
    {
        _points = Array.Empty<UGPoint2>();
    }

    public UGPolyline2(IEnumerable<UGPoint2> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        _points = points.ToArray();
    }

    public static UGPolyline2 Empty => new();

    public int Count => _points.Length;

    public bool IsEmpty => _points.Length == 0;

    public UGPoint2 this[int index] => _points[index];

    public ReadOnlySpan<UGPoint2> Points => _points;

    public UGBounds2 Bounds => UGBounds2.FromPoints(_points);

    public double Length
    {
        get
        {
            if (_points.Length < 2)
                return 0d;

            var total = 0d;
            for (var i = 0; i < _points.Length - 1; i++)
            {
                var a = _points[i];
                var b = _points[i + 1];
                total += a.DistanceTo(b);
            }

            return total;
        }
    }
}
