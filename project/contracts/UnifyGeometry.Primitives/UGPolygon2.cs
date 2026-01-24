namespace UnifyGeometry;

public sealed class UGPolygon2
{
    private readonly UGPoint2[] _vertices;

    public UGPolygon2()
    {
        _vertices = Array.Empty<UGPoint2>();
    }

    public UGPolygon2(IEnumerable<UGPoint2> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        _vertices = vertices.ToArray();
    }

    public static UGPolygon2 Empty => new();

    public int Count => _vertices.Length;

    public bool IsEmpty => _vertices.Length == 0;

    public UGPoint2 this[int index] => _vertices[index];

    public ReadOnlySpan<UGPoint2> Vertices => _vertices;

    public UGBounds2 Bounds => UGBounds2.FromPoints(_vertices);
}
