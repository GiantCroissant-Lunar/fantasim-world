using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineSplitByIntersectionsTests
{
    [Fact]
    public void SplitByIntersections_SimpleCrossing_SplitsIntoTwo()
    {
        var a = new Polyline2(new[] { new Point2(0, 0), new Point2(10, 0) });
        var b = new Polyline2(new[] { new Point2(5, -5), new Point2(5, 5) });

        var parts = PolylineSplitAtIntersections.SplitByIntersections(a, b);

        Assert.Equal(2, parts.Count);
        Assert.Equal(0d, parts[0][0].X, 12);
        Assert.Equal(5d, parts[0][^1].X, 12);
        Assert.Equal(5d, parts[1][0].X, 12);
        Assert.Equal(10d, parts[1][^1].X, 12);
    }

    [Fact]
    public void SplitByIntersections_NoIntersection_ReturnsSingle()
    {
        var a = new Polyline2(new[] { new Point2(0, 0), new Point2(10, 0) });
        var b = new Polyline2(new[] { new Point2(0, 5), new Point2(10, 5) });

        var parts = PolylineSplitAtIntersections.SplitByIntersections(a, b);
        Assert.Single(parts);
    }

    [Fact]
    public void SplitByIntersections_CollinearOverlap_SplitsAtOverlapEndpoints()
    {
        var a = new Polyline2(new[] { new Point2(0, 0), new Point2(10, 0) });
        var b = new Polyline2(new[] { new Point2(3, 0), new Point2(7, 0) });

        var parts = PolylineSplitAtIntersections.SplitByIntersections(a, b);

        Assert.Equal(3, parts.Count);

        Assert.Equal(0d, parts[0][0].X, 12);
        Assert.Equal(3d, parts[0][^1].X, 12);

        Assert.Equal(3d, parts[1][0].X, 12);
        Assert.Equal(7d, parts[1][^1].X, 12);

        Assert.Equal(7d, parts[2][0].X, 12);
        Assert.Equal(10d, parts[2][^1].X, 12);
    }
}
