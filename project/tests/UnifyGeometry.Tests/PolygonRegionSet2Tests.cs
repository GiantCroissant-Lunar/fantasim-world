using UnifyGeometry;

namespace UnifyGeometry.Tests;

public sealed class PolygonRegionSet2Tests
{
    [Fact]
    public void Empty_IsEmptyAndHasCountZero()
    {
        var set = PolygonRegionSet2.Empty;
        Assert.True(set.IsEmpty);
        Assert.Equal(0, set.Count);
        Assert.Empty(set.Regions);
    }

    [Fact]
    public void Construct_WithRegions_PreservesCountAndIndexing()
    {
        var r0 = new PolygonRegion2(
            new Polygon2(new[]
            {
                new Point2(0, 0),
                new Point2(1, 0),
                new Point2(1, 1),
            }),
            Array.Empty<Polygon2>());

        var r1 = new PolygonRegion2(
            new Polygon2(new[]
            {
                new Point2(10, 10),
                new Point2(11, 10),
                new Point2(11, 11),
            }),
            Array.Empty<Polygon2>());

        var set = new PolygonRegionSet2(new[] { r0, r1 });

        Assert.False(set.IsEmpty);
        Assert.Equal(2, set.Count);
        Assert.Same(r0, set[0]);
        Assert.Same(r1, set[1]);
        Assert.Equal(2, set.Regions.Count);
    }
}
