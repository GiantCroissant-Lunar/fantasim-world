using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolygonProjectPointTests
{
    [Fact]
    public void ProjectPoint_Inside_ReturnsBoundaryPointAndInsideSign()
    {
        var square = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        var hit = PolygonProjectPoint.ProjectPoint(square, new Point2(1, 1));

        Assert.False(hit.IsEmpty);
        Assert.Equal(-1, hit.ContainsSign);
        Assert.Equal(1d, hit.DistanceSquared, 12);
        Assert.True(Polygon2Ops.ContainsPoint(square, hit.Point));
    }

    [Fact]
    public void ProjectPoint_Outside_ReturnsBoundaryPointAndOutsideSign()
    {
        var square = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        var hit = PolygonProjectPoint.ProjectPoint(square, new Point2(-1, 1));

        Assert.False(hit.IsEmpty);
        Assert.Equal(1, hit.ContainsSign);
        Assert.Equal(1d, hit.DistanceSquared, 12);
        Assert.True(Polygon2Ops.ContainsPoint(square, hit.Point));
    }

    [Fact]
    public void ProjectPoint_OnBoundary_ReturnsZeroDistanceAndZeroSign()
    {
        var square = new Polygon2(new[]
        {
            new Point2(0, 0),
            new Point2(2, 0),
            new Point2(2, 2),
            new Point2(0, 2),
        });

        var hit = PolygonProjectPoint.ProjectPoint(square, new Point2(2, 1));

        Assert.False(hit.IsEmpty);
        Assert.Equal(0, hit.ContainsSign);
        Assert.Equal(0d, hit.DistanceSquared, 12);
        Assert.Equal(new Point2(2, 1), hit.Point);
    }
}
