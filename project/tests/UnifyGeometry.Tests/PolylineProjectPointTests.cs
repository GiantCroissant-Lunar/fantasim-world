using UnifyGeometry;
using UnifyGeometry.Operations;

namespace UnifyGeometry.Tests;

public sealed class PolylineProjectPointTests
{
    [Fact]
    public void ClosestPoint_OnSingleSegment_ProjectsOrthogonally()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var p = new UGPoint2(5, 2);

        var proj = PolylineProjectPoint.ProjectPoint(poly, p);

        Assert.False(proj.IsEmpty);
        Assert.Equal(5d, proj.Point.X, 12);
        Assert.Equal(0d, proj.Point.Y, 12);
        Assert.Equal(0, proj.SegmentIndex);
        Assert.Equal(0.5d, proj.SegmentT, 12);
        Assert.Equal(5d, proj.DistanceAlong, 12);
        Assert.Equal(4d, proj.DistanceSquared, 12);
        Assert.Equal(1, proj.SideSign);
    }

    [Fact]
    public void ProjectPoint_ClampsBeforeStart()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var p = new UGPoint2(-3, 4);

        var proj = PolylineProjectPoint.ProjectPoint(poly, p);

        Assert.Equal(0d, proj.Point.X, 12);
        Assert.Equal(0d, proj.Point.Y, 12);
        Assert.Equal(0d, proj.DistanceAlong, 12);
        Assert.Equal(0d, proj.SegmentT, 12);
        Assert.Equal(1, proj.SideSign);
    }

    [Fact]
    public void ProjectPoint_ClampsAfterEnd()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var p = new UGPoint2(12, -1);

        var proj = PolylineProjectPoint.ProjectPoint(poly, p);

        Assert.Equal(10d, proj.Point.X, 12);
        Assert.Equal(0d, proj.Point.Y, 12);
        Assert.Equal(10d, proj.DistanceAlong, 12);
        Assert.Equal(1d, proj.SegmentT, 12);
        Assert.Equal(-1, proj.SideSign);
    }

    [Fact]
    public void ProjectPoint_MultiSegment_ReportsArcLengthCoordinate()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(6, 0), new UGPoint2(6, 8) });
        var p = new UGPoint2(8, 3);

        var proj = PolylineProjectPoint.ProjectPoint(poly, p);

        Assert.Equal(6d, proj.Point.X, 12);
        Assert.Equal(3d, proj.Point.Y, 12);
        Assert.Equal(1, proj.SegmentIndex);
        Assert.Equal(6d + 3d, proj.DistanceAlong, 12);
        Assert.Equal(4d, proj.DistanceSquared, 12);
        Assert.Equal(-1, proj.SideSign);
    }

    [Fact]
    public void ProjectPoint_DegenerateSegment_StillWorks()
    {
        var poly = new UGPolyline2(new[] { new UGPoint2(0, 0), new UGPoint2(0, 0), new UGPoint2(10, 0) });
        var p = new UGPoint2(5, 1);

        var proj = PolylineProjectPoint.ProjectPoint(poly, p);

        Assert.Equal(5d, proj.Point.X, 12);
        Assert.Equal(0d, proj.Point.Y, 12);
        Assert.Equal(5d, proj.DistanceAlong, 12);
        Assert.Equal(1, proj.SideSign);
    }
}
