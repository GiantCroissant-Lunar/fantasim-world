using UnifyGeometry;

namespace UnifyGeometry.Tests;

public sealed class PrimitivesTests
{
    [Fact]
    public void Point2_DistanceTo_IsSymmetric()
    {
        var a = new UGPoint2(0, 0);
        var b = new UGPoint2(3, 4);

        Assert.Equal(5d, a.DistanceTo(b), 12);
        Assert.Equal(5d, b.DistanceTo(a), 12);
    }

    [Fact]
    public void Polyline_Length_IsSumOfSegments()
    {
        var poly = new UGPolyline2(new[]
        {
            new UGPoint2(0, 0),
            new UGPoint2(3, 4),
            new UGPoint2(6, 8),
        });

        Assert.Equal(10d, poly.Length, 12);
    }

    [Fact]
    public void Polyline_Empty_LengthIsZero()
    {
        var poly = UGPolyline2.Empty;
        Assert.Equal(0d, poly.Length);
        Assert.True(poly.IsEmpty);
    }

    [Fact]
    public void Bounds_FromPoints_ComputesMinMax()
    {
        var poly = new UGPolyline2(new[]
        {
            new UGPoint2(-1, 2),
            new UGPoint2(5, -3),
            new UGPoint2(4, 10),
        });

        Assert.False(poly.Bounds.IsEmpty);
        Assert.Equal(new UGPoint2(-1, -3), poly.Bounds.Min);
        Assert.Equal(new UGPoint2(5, 10), poly.Bounds.Max);
    }
}
