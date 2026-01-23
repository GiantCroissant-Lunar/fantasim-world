using Plate.Topology.Contracts.Geometry;

namespace UnifyGeometry.Adapters.PlateTopology;

public static class PlateTopologyGeometryAdapters
{
    public static UGPoint2 ToUnify(Point2D point)
        => new(point.X, point.Y);

    public static Point2D ToPlate(UGPoint2 point)
        => new(point.X, point.Y);

    public static UGSegment2 ToUnify(LineSegment segment)
        => new(ToUnify(segment.Start), ToUnify(segment.End));

    public static LineSegment ToPlate(UGSegment2 segment)
        => new(ToPlate(segment.Start), ToPlate(segment.End));

    public static UGPolyline2 ToUnify(Polyline polyline)
    {
        ArgumentNullException.ThrowIfNull(polyline);
        return new UGPolyline2(polyline.Points.Select(ToUnify));
    }

    public static Polyline ToPlate(UGPolyline2 polyline)
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
