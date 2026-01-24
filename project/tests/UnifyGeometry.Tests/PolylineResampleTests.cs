using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineResampleTests
{
    [Fact]
    public void Resample_PointCountZero_ReturnsEmpty()
    {
        var poly = new Polyline2(new[] { new Point2(0, 0), new Point2(10, 0) });
        var r = PolylineResample.ByPointCount(poly, 0);
        Assert.True(r.IsEmpty);
    }

    [Fact]
    public void Resample_PointCountTwo_PreservesEndpoints()
    {
        var poly = new Polyline2(new[] { new Point2(0, 0), new Point2(10, 0), new Point2(20, 0) });
        var r = PolylineResample.ByPointCount(poly, 2);
        Assert.Equal(2, r.Count);
        Assert.Equal(poly[0], r[0]);
        Assert.Equal(poly[^1], r[^1]);
    }

    [Fact]
    public void Resample_DistributesEvenlyAlongLength()
    {
        var poly = new Polyline2(new[] { new Point2(0, 0), new Point2(10, 0) });
        var r = PolylineResample.ByPointCount(poly, 5);

        Assert.Equal(5, r.Count);
        Assert.Equal(new Point2(0, 0), r[0]);
        Assert.Equal(new Point2(10, 0), r[^1]);
        Assert.Equal(new Point2(2.5, 0), r[1]);
        Assert.Equal(new Point2(5, 0), r[2]);
        Assert.Equal(new Point2(7.5, 0), r[3]);
    }
}
