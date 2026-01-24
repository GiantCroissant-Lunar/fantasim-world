namespace UnifyGeometry;

public sealed class UGPolygonRegion2
{
    public UGPolygonRegion2(UGPolygon2 outer, IReadOnlyList<UGPolygon2> holes)
    {
        ArgumentNullException.ThrowIfNull(outer);
        ArgumentNullException.ThrowIfNull(holes);

        Outer = outer;
        Holes = holes;
    }

    public UGPolygon2 Outer { get; }

    public IReadOnlyList<UGPolygon2> Holes { get; }

    public bool IsEmpty => Outer.IsEmpty;

    public static UGPolygonRegion2 Empty => new(UGPolygon2.Empty, Array.Empty<UGPolygon2>());
}
