using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonRegion2Tests
{
    [Fact]
    public void RegionArea_OuterMinusHole()
    {
        var outer = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(10, 0),
            new UGPoint2(10, 10),
            new UGPoint2(0, 10),
        });

        var hole = new UGPolygon2(new[]
        {
            new UGPoint2(4, 4),
            new UGPoint2(6, 4),
            new UGPoint2(6, 6),
            new UGPoint2(4, 6),
        });

        var region = new UGPolygonRegion2(outer, new[] { hole });
        Assert.Equal(100d - 4d, PolygonRegion2.Area(region), 12);
    }

    [Fact]
    public void ContainsPoint_InsideHole_IsFalse()
    {
        var outer = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(10, 0),
            new UGPoint2(10, 10),
            new UGPoint2(0, 10),
        });

        var hole = new UGPolygon2(new[]
        {
            new UGPoint2(4, 4),
            new UGPoint2(6, 4),
            new UGPoint2(6, 6),
            new UGPoint2(4, 6),
        });

        var region = new UGPolygonRegion2(outer, new[] { hole });

        Assert.True(PolygonRegion2.ContainsPoint(region, new UGPoint2(1, 1)));
        Assert.False(PolygonRegion2.ContainsPoint(region, new UGPoint2(5, 5)));
        Assert.False(PolygonRegion2.ContainsPoint(region, new UGPoint2(11, 5)));
    }
}
