using UnifyGeometry;

namespace UnifyGeometry.Tests;

public sealed class PrimitivesTests
{
    [Fact]
    public void Point2_DistanceTo_IsSymmetric()
    {
        var a = new Point2(0, 0);
        var b = new Point2(3, 4);

        Assert.Equal(5d, a.DistanceTo(b), 12);
        Assert.Equal(5d, b.DistanceTo(a), 12);
    }

    [Fact]
    public void Polyline_Length_IsSumOfSegments()
    {
        var poly = new Polyline2(new[]
        {
            new Point2(0, 0),
            new Point2(3, 4),
            new Point2(6, 8),
        });

        Assert.Equal(10d, poly.Length, 12);
    }

    [Fact]
    public void Polyline_Empty_LengthIsZero()
    {
        var poly = Polyline2.Empty;
        Assert.Equal(0d, poly.Length);
        Assert.True(poly.IsEmpty);
    }

    [Fact]
    public void Bounds_FromPoints_ComputesMinMax()
    {
        var poly = new Polyline2(new[]
        {
            new Point2(-1, 2),
            new Point2(5, -3),
            new Point2(4, 10),
        });

        Assert.False(poly.Bounds.IsEmpty);
        Assert.Equal(new Point2(-1, -3), poly.Bounds.Min);
        Assert.Equal(new Point2(5, 10), poly.Bounds.Max);
    }
}
