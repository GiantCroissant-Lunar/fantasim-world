using Plate.Topology.Contracts.Geometry;

namespace UnifyGeometry.Adapters.PlateTopology;

public static class PlateTopologyGeometryAdapters
{
    public static Point2 ToUnify(Point2D point)
        => new(point.X, point.Y);

    public static Point2D ToPlate(Point2 point)
        => new(point.X, point.Y);

    public static Segment2 ToUnify(LineSegment segment)
        => new(ToUnify(segment.Start), ToUnify(segment.End));

    public static LineSegment ToPlate(Segment2 segment)
        => new(ToPlate(segment.Start), ToPlate(segment.End));

    public static Polyline2 ToUnify(Polyline polyline)
    {
        ArgumentNullException.ThrowIfNull(polyline);
        return new Polyline2(polyline.Points.Select(ToUnify));
    }

    public static Polyline ToPlate(Polyline2 polyline)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (polyline.IsEmpty)
            return new Polyline();

        var points = new List<Point2D>(polyline.Count);
        foreach (var p in polyline.Points)
        {
            points.Add(ToPlate(p));
        }

        return new Polyline(points);
    }
}
