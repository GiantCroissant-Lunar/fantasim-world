namespace UnifyGeometry;

public sealed class UGPolygonRegionSet2
{
    private readonly UGPolygonRegion2[] _regions;

    public UGPolygonRegionSet2()
    {
        _regions = Array.Empty<UGPolygonRegion2>();
    }

    public UGPolygonRegionSet2(IEnumerable<UGPolygonRegion2> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);
        _regions = regions.ToArray();
    }

    public static UGPolygonRegionSet2 Empty => new();

    public int Count => _regions.Length;

    public bool IsEmpty => _regions.Length == 0;

    public UGPolygonRegion2 this[int index] => _regions[index];

    public IReadOnlyList<UGPolygonRegion2> Regions => _regions;
}
