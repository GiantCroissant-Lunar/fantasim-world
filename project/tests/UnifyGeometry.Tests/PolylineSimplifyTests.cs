using UnifyGeometry;
using UnifyGeometry.Operators;

namespace UnifyGeometry.Tests;

public sealed class PolylineSimplifyTests
{
    [Fact]
    public void Simplify_EpsilonZero_ReturnsOriginal()
    {
        var poly = new UGPolyline2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(1, 0.1),
            new UGPoint2(2, 0),
        });

        var s = PolylineSimplify.RamerDouglasPeucker(poly, 0);
        Assert.Equal(poly.Count, s.Count);
    }

    [Fact]
    public void Simplify_RemovesPointsWithinEpsilon()
    {
        var poly = new UGPolyline2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(1, 0.01),
            new UGPoint2(2, 0),
        });

        var s = PolylineSimplify.RamerDouglasPeucker(poly, 0.1);
        Assert.Equal(2, s.Count);
        Assert.Equal(poly[0], s[0]);
        Assert.Equal(poly[^1], s[^1]);
    }
}
