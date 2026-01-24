using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonNormalizeTests
{
    [Fact]
    public void Simplify_RemovesClosingDuplicateAndCollinear()
    {
        var poly = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(4, 0), // collinear
            new UGPoint2(4, 2),
            new UGPoint2(0, 2),
            new UGPoint2(0, 0), // closing dup
        });

        var s = PolygonSimplify.RemoveCollinearAndDuplicates(poly);
        Assert.Equal(4, s.Count);
        Assert.Equal(8d, Math.Abs(Polygon2.SignedArea(s)), 12);
    }

    [Fact]
    public void EnsureCounterClockwise_ReversesIfNeeded()
    {
        var squareCW = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(0, 2),
            new UGPoint2(2, 2),
            new UGPoint2(2, 0),
        });

        var ccw = PolygonNormalize.EnsureCounterClockwise(squareCW);
        Assert.False(ccw.IsEmpty);
        Assert.False(Polygon2.IsClockwise(ccw));
        Assert.Equal(4d, Polygon2.SignedArea(ccw), 12);
    }

    [Fact]
    public void EnsureClockwise_ReversesIfNeeded()
    {
        var squareCCW = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(2, 0),
            new UGPoint2(2, 2),
            new UGPoint2(0, 2),
        });

        var cw = PolygonNormalize.EnsureClockwise(squareCCW);
        Assert.False(cw.IsEmpty);
        Assert.True(Polygon2.IsClockwise(cw));
        Assert.Equal(-4d, Polygon2.SignedArea(cw), 12);
    }
}
