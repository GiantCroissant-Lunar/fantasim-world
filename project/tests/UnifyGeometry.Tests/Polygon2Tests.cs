using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class Polygon2Tests
{
    [Fact]
    public void SignedArea_UnitSquare_IsPositive()
    {
        var square = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(1, 0),
            new Point2(1, 1),
            new Point2(0, 1),
        });

        Assert.Equal(1d, Polygon2Ops.SignedArea(square), 12);
        Assert.False(Polygon2Ops.IsClockwise(square));
    }

    [Fact]
    public void SignedArea_Reversed_IsNegative()
    {
        var square = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(0, 1),
            new Point2(1, 1),
            new Point2(1, 0),
        });

        Assert.Equal(-1d, Polygon2Ops.SignedArea(square), 12);
        Assert.True(Polygon2Ops.IsClockwise(square));
    }

    [Fact]
    public void ContainsPoint_InsideOutsideAndBoundary()
    {
        var square = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(10, 0),
            new Point2(10, 10),
            new Point2(0, 10),
        });

        Assert.True(Polygon2Ops.ContainsPoint(square, new Point2(5, 5)));
        Assert.False(Polygon2Ops.ContainsPoint(square, new Point2(11, 5)));
        Assert.True(Polygon2Ops.ContainsPoint(square, new Point2(10, 5))); // boundary
    }
}
