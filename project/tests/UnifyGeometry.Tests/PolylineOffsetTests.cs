using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineOffsetTests
{
    [Fact]
    public void Offset_StraightLine_Left()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var o = PolylineOffset.ByDistance(poly, 2, PolylineOffsetSide.Left);

        Assert.Equal(2, o.Count);
        Assert.Equal(new UGPoint2(0, 2), o[0]);
        Assert.Equal(new UGPoint2(10, 2), o[1]);
    }

    [Fact]
    public void Offset_StraightLine_Right()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var o = PolylineOffset.ByDistance(poly, 2, PolylineOffsetSide.Right);

        Assert.Equal(2, o.Count);
        Assert.Equal(new UGPoint2(0, -2), o[0]);
        Assert.Equal(new UGPoint2(10, -2), o[1]);
    }

    [Fact]
    public void Offset_LShape_MiterJoin()
    {
        var poly = new UGPolyline2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(10, 0),
            new UGPoint2(10, 10),
        });

        var o = PolylineOffset.ByDistance(poly, 1, PolylineOffsetSide.Left);

        Assert.Equal(3, o.Count);
        Assert.Equal(new UGPoint2(0, 1), o[0]);
        Assert.Equal(new UGPoint2(9, 1), o[1]);
        Assert.Equal(new UGPoint2(9, 10), o[2]);
    }

    [Fact]
    public void Offset_Colinear_FallsBackCleanly()
    {
        var poly = new UGPolyline2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(10, 0),
            new UGPoint2(20, 0),
        });

        var o = PolylineOffset.ByDistance(poly, 1, PolylineOffsetSide.Left);
        Assert.Equal(3, o.Count);
        Assert.Equal(new UGPoint2(0, 1), o[0]);
        Assert.Equal(new UGPoint2(10, 1), o[1]);
        Assert.Equal(new UGPoint2(20, 1), o[2]);
    }

    [Fact]
    public void Offset_MiterLimit_Clamps()
    {
        var poly = new UGPolyline2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(10, 0),
            new UGPoint2(10, 0.1),
        });

        var dist = 1.0;
        var miterLimit = 2.0;
        var o = PolylineOffset.ByDistance(poly, dist, PolylineOffsetSide.Left, miterLimit: miterLimit);

        Assert.Equal(3, o.Count);

        var mid = o[1];
        var dx = mid.X - poly[1].X;
        var dy = mid.Y - poly[1].Y;
        var miterLen = Math.Sqrt((dx * dx) + (dy * dy));

        Assert.True(miterLen <= (dist * miterLimit) + 1e-9);
    }
}
