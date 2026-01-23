using UnifyGeometry;
using UnifyGeometry.Operators;

namespace UnifyGeometry.Tests;

public sealed class PolylineResampleTests
{
    [Fact]
    public void Resample_PointCountZero_ReturnsEmpty()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var r = PolylineResample.ByPointCount(poly, 0);
        Assert.True(r.IsEmpty);
    }

    [Fact]
    public void Resample_PointCountTwo_PreservesEndpoints()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0), new UGPoint2(20, 0) });
        var r = PolylineResample.ByPointCount(poly, 2);
        Assert.Equal(2, r.Count);
        Assert.Equal(poly[0], r[0]);
        Assert.Equal(poly[^1], r[^1]);
    }

    [Fact]
    public void Resample_DistributesEvenlyAlongLength()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var r = PolylineResample.ByPointCount(poly, 5);

        Assert.Equal(5, r.Count);
        Assert.Equal(new UGPoint2(0, 0), r[0]);
        Assert.Equal(new UGPoint2(10, 0), r[^1]);
        Assert.Equal(new UGPoint2(2.5, 0), r[1]);
        Assert.Equal(new UGPoint2(5, 0), r[2]);
        Assert.Equal(new UGPoint2(7.5, 0), r[3]);
    }
}
