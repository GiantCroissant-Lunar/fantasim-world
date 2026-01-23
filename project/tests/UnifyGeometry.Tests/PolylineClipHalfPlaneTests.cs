using UnifyGeometry;
using UnifyGeometry.Operators;

namespace UnifyGeometry.Tests;

public sealed class PolylineClipHalfPlaneTests
{
    [Fact]
    public void Clip_XGreaterOrEqualZero()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(-1, 0), new UGPoint2(1, 0) });
        var clipped = PolylineClipHalfPlane.ToHalfPlane(poly, pointOnBoundary: new UGPoint2(0, 0), inwardNormal: new UGPoint2(1, 0));

        Assert.Single(clipped);
        Assert.Equal(2, clipped[0].Count);
        Assert.Equal(0d, clipped[0][0].X, 12);
        Assert.Equal(0d, clipped[0][0].Y, 12);
        Assert.Equal(1d, clipped[0][1].X, 12);
        Assert.Equal(0d, clipped[0][1].Y, 12);
    }

    [Fact]
    public void Clip_Outside_ReturnsEmpty()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(-2, 0), new UGPoint2(-1, 1) });
        var clipped = PolylineClipHalfPlane.ToHalfPlane(poly, pointOnBoundary: new UGPoint2(0, 0), inwardNormal: new UGPoint2(1, 0));
        Assert.Empty(clipped);
    }

    [Fact]
    public void Clip_ZigZag_DoesNotThrow()
    {
        var poly = new UGPolyline2(new[]
        {
            new UGPoint2(1, 0),
            new UGPoint2(-1, 0),
            new UGPoint2(1, 0),
        });

        var clipped = PolylineClipHalfPlane.ToHalfPlane(poly, pointOnBoundary: new UGPoint2(0, 0), inwardNormal: new UGPoint2(1, 0));

        // The polyline exits the half-plane and re-enters, so clipping returns two pieces.
        Assert.Equal(2, clipped.Count);
        Assert.Equal(2, clipped[0].Count);
        Assert.Equal(2, clipped[1].Count);
        Assert.Equal(1d, clipped[0][0].X, 12);
        Assert.Equal(0d, clipped[0][^1].X, 12);
        Assert.Equal(0d, clipped[1][0].X, 12);
        Assert.Equal(1d, clipped[1][^1].X, 12);
    }
}
