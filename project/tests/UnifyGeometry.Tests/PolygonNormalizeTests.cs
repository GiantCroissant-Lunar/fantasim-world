using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonNormalizeTests
{
    [Fact]
    public void Simplify_RemovesClosingDuplicateAndCollinear()
    {
        var poly = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(4, 0), // collinear
            new Point2(4, 2),
            new Point2(0, 2),
            new Point2(0, 0), // closing dup
        });

        var s = PolygonSimplify.RemoveCollinearAndDuplicates(poly);
        Assert.Equal(4, s.Count);
        Assert.Equal(8d, Math.Abs(Polygon2Ops.SignedArea(s)), 12);
    }

    [Fact]
    public void EnsureCounterClockwise_ReversesIfNeeded()
    {
        var squareCW = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(0, 2),
            new Point2(2, 2),
            new Point2(2, 0),
        });

        var ccw = PolygonNormalize.EnsureCounterClockwise(squareCW);
        Assert.False(ccw.IsEmpty);
        Assert.False(Polygon2Ops.IsClockwise(ccw));
        Assert.Equal(4d, Polygon2Ops.SignedArea(ccw), 12);
    }

    [Fact]
    public void EnsureClockwise_ReversesIfNeeded()
    {
        var squareCCW = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        var cw = PolygonNormalize.EnsureClockwise(squareCCW);
        Assert.False(cw.IsEmpty);
        Assert.True(Polygon2Ops.IsClockwise(cw));
        Assert.Equal(-4d, Polygon2Ops.SignedArea(cw), 12);
    }
}
