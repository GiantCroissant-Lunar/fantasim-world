using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonRegion2Tests
{
    [Fact]
    public void RegionArea_OuterMinusHole()
    {
        var outer = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(10, 0),
            new Point2(10, 10),
            new Point2(0, 10),
        });

        var hole = new Polygon2(new[]
        {
            new Point2(4, 4),
            new Point2(6, 4),
            new Point2(6, 6),
            new Point2(4, 6),
        });

        var region = new PolygonRegion2(outer, new[] { hole });
        Assert.Equal(100d - 4d, PolygonRegion2Ops.Area(region), 12);
    }

    [Fact]
    public void ContainsPoint_InsideHole_IsFalse()
    {
        var outer = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(10, 0),
            new Point2(10, 10),
            new Point2(0, 10),
        });

        var hole = new Polygon2(new[]
        {
            new Point2(4, 4),
            new Point2(6, 4),
            new Point2(6, 6),
            new Point2(4, 6),
        });

        var region = new PolygonRegion2(outer, new[] { hole });

        Assert.True(PolygonRegion2Ops.ContainsPoint(region, new Point2(1, 1)));
        Assert.False(PolygonRegion2Ops.ContainsPoint(region, new Point2(5, 5)));
        Assert.False(PolygonRegion2Ops.ContainsPoint(region, new Point2(11, 5)));
    }
}
