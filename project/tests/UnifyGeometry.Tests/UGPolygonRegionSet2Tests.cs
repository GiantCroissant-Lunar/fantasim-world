using UnifyGeometry;

namespace UnifyGeometry.Tests;

public sealed class UGPolygonRegionSet2Tests
{
    [Fact]
    public void Empty_IsEmptyAndHasCountZero()
    {
        var set = UGPolygonRegionSet2.Empty;
        Assert.True(set.IsEmpty);
        Assert.Equal(0, set.Count);
        Assert.Empty(set.Regions);
    }

    [Fact]
    public void Construct_WithRegions_PreservesCountAndIndexing()
    {
        var r0 = new UGPolygonRegion2(
            new UGPolygon2(new[]
            {
                new UGPoint2(0, 0),
                new UGPoint2(1, 0),
                new UGPoint2(1, 1),
            }),
            Array.Empty<UGPolygon2>());

        var r1 = new UGPolygonRegion2(
            new UGPolygon2(new[]
            {
                new UGPoint2(10, 10),
                new UGPoint2(11, 10),
                new UGPoint2(11, 11),
            }),
            Array.Empty<UGPolygon2>());

        var set = new UGPolygonRegionSet2(new[] { r0, r1 });

        Assert.False(set.IsEmpty);
        Assert.Equal(2, set.Count);
        Assert.Same(r0, set[0]);
        Assert.Same(r1, set[1]);
        Assert.Equal(2, set.Regions.Count);
    }
}
