using Plate.Topology.Contracts.Geometry;
using UnifyGeometry.Adapters.PlateTopology;

namespace UnifyGeometry.Tests;

public sealed class PlateTopologyAdaptersTests
{
    [Fact]
    public void Point_RoundTrips()
    {
        var p = new Point2D(1.5, -2.25);
        var u = PlateTopologyGeometryAdapters.ToUnify(p);
        var back = PlateTopologyGeometryAdapters.ToPlate(u);
        Assert.Equal(p, back);
    }

    [Fact]
    public void Segment_RoundTrips()
    {
        var s = new LineSegment(new Point2D(0, 0), new Point2D(3, 4));
        var u = PlateTopologyGeometryAdapters.ToUnify(s);
        var back = PlateTopologyGeometryAdapters.ToPlate(u);
        Assert.Equal(s.Start, back.Start);
        Assert.Equal(s.End, back.End);
    }

    [Fact]
    public void Polyline_RoundTrips()
    {
        var p = new Polyline(new[]
        {
            new Point2D(0, 0),
            new Point2D(1, 2),
            new Point2D(3, 5),
        });

        var u = PlateTopologyGeometryAdapters.ToUnify(p);
        var back = PlateTopologyGeometryAdapters.ToPlate(u);

        Assert.Equal(p.PointCount, back.PointCount);
        for (var i = 0; i < p.PointCount; i++)
        {
            Assert.Equal(p.Points[i], back.Points[i]);
        }
    }
}
