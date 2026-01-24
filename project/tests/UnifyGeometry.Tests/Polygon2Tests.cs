using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class Polygon2Tests
{
    [Fact]
    public void SignedArea_UnitSquare_IsPositive()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(1, 0),
            new UGPoint2(1, 1),
            new UGPoint2(0, 1),
        });

        Assert.Equal(1d, Polygon2.SignedArea(square), 12);
        Assert.False(Polygon2.IsClockwise(square));
    }

    [Fact]
    public void SignedArea_Reversed_IsNegative()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(0, 1),
            new UGPoint2(1, 1),
            new UGPoint2(1, 0),
        });

        Assert.Equal(-1d, Polygon2.SignedArea(square), 12);
        Assert.True(Polygon2.IsClockwise(square));
    }

    [Fact]
    public void ContainsPoint_InsideOutsideAndBoundary()
    {
        var square = new UGPolygon2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(10, 0),
            new UGPoint2(10, 10),
            new UGPoint2(0, 10),
        });

        Assert.True(Polygon2.ContainsPoint(square, new UGPoint2(5, 5)));
        Assert.False(Polygon2.ContainsPoint(square, new UGPoint2(11, 5)));
        Assert.True(Polygon2.ContainsPoint(square, new UGPoint2(10, 5))); // boundary
    }
}
