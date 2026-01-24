using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineSimplifyTests
{
    [Fact]
    public void Simplify_EpsilonZero_ReturnsOriginal()
    {
        var poly = new Polyline2(new[]
        {
            new Point2(0, 0),
            new Point2(1, 0.1),
            new Point2(2, 0),
        });

        var s = PolylineSimplify.RamerDouglasPeucker(poly, 0);
        Assert.Equal(poly.Count, s.Count);
    }

    [Fact]
    public void Simplify_RemovesPointsWithinEpsilon()
    {
        var poly = new Polyline2(new[]
        {
            new Point2(0, 0),
            new Point2(1, 0.01),
            new Point2(2, 0),
        });

        var s = PolylineSimplify.RamerDouglasPeucker(poly, 0.1);
        Assert.Equal(2, s.Count);
        Assert.Equal(poly[0], s[0]);
        Assert.Equal(poly[^1], s[^1]);
    }
}
