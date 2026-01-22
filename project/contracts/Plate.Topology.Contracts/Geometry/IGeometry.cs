namespace Plate.Topology.Contracts.Geometry;

/// <summary>
/// Enumerates the types of geometry supported by the topology system.
/// </summary>
public enum GeometryType
{
    /// <summary>
    /// Zero-dimensional point geometry (e.g., junction location).
    /// </summary>
    Point,

    /// <summary>
    /// One-dimensional line segment geometry connecting two points.
    /// </summary>
    LineSegment,

    /// <summary>
    /// One-dimensional polyline geometry consisting of multiple connected line segments.
    /// </summary>
    Polyline
}

/// <summary>
/// Abstract geometry contract for plate topology per FR-004.
///
/// This interface defines the minimal geometric primitives required for topology
/// without committing to any specific spatial substrate or coordinate system.
/// Implementations can represent geometry using line segments, curves, parametric
/// definitions, or any other geometric representation suitable for the domain.
///
/// Key principles:
/// - Substrate-agnostic: No assumption about Voronoi, DGGS, latitude/longitude, etc.
/// - Topology-first: Geometry supports the boundary graph structure without dictating
///   spatial sampling or cell meshes.
/// - Abstract: Concrete representation details are left to implementations.
///
/// Derived products (cell meshes, overlays, sampling grids) are computed from
/// topology state and geometry, not stored as truth dependencies.
/// </summary>
public interface IGeometry
{
    /// <summary>
    /// Gets the spatial dimension of this geometry.
    ///
    /// Common values:
    /// - 0: Point geometry (e.g., junction location)
    /// - 1: Linear geometry (e.g., boundary segment/curve)
    /// - Future: 2 for surface geometry if needed
    /// </summary>
    int Dimension { get; }

    /// <summary>
    /// Gets the concrete type of this geometry.
    ///
    /// Allows polymorphic discrimination between different geometry types
    /// that may share the same dimension (e.g., LineSegment vs Polyline both have Dimension=1).
    /// </summary>
    GeometryType GeometryType { get; }

    /// <summary>
    /// Determines whether this geometry is empty (has no spatial extent).
    ///
    /// Empty geometries can represent degenerate cases such as collapsed boundaries
    /// or null junctions in specific topological configurations.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Computes an approximate spatial length for this geometry.
    ///
    /// For point geometry (Dimension 0), length is zero.
    /// For linear geometry (Dimension 1), represents the curve/segment length.
    ///
    /// This is an abstract measure; the actual units and metric are implementation-defined
    /// and substrate-agnostic. Different implementations may use different metrics
    /// (e.g., great-circle distance, Euclidean distance, parametric arc length).
    /// </summary>
    double ComputeLength();

    /// <summary>
    /// Creates a deep copy of this geometry.
    ///
    /// Returns a new instance with the same spatial properties but independent
    /// of the original object, ensuring immutability semantics when needed.
    /// </summary>
    IGeometry Clone();
}

/// <summary>
/// Concrete 2D point geometry implementing IGeometry per FR-004.
///
/// Represents a zero-dimensional point location in a 2D coordinate system.
/// Points are used for junction locations and other discrete spatial features.
///
/// Follows readonly record struct pattern for value-type semantics and immutability.
/// </summary>
public readonly record struct Point2D : IGeometry
{
    /// <summary>
    /// The X coordinate of the point.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// The Y coordinate of the point.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Gets a value indicating whether this point is empty (NaN coordinates).
    /// </summary>
    public bool IsEmpty => double.IsNaN(X) || double.IsNaN(Y);

    /// <summary>
    /// Gets the spatial dimension of this geometry (always 0 for points).
    /// </summary>
    public int Dimension => 0;

    /// <summary>
    /// Gets the concrete type of this geometry (always Point for Point2D).
    /// </summary>
    GeometryType IGeometry.GeometryType => GeometryType.Point;

    /// <summary>
    /// Initializes a new instance of the Point2D struct with the specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Point2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets an empty point with NaN coordinates.
    /// </summary>
    public static Point2D Empty => new(double.NaN, double.NaN);

    /// <summary>
    /// Creates a new point at the origin (0, 0).
    /// </summary>
    public static Point2D Origin => new(0, 0);

    /// <summary>
    /// Computes the Euclidean distance between this point and another point.
    /// </summary>
    /// <param name="other">The other point.</param>
    /// <returns>The Euclidean distance.</returns>
    public double DistanceTo(Point2D other)
    {
        if (IsEmpty || other.IsEmpty)
            return double.NaN;

        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Computes an approximate spatial length for this geometry (always 0 for points).
    /// </summary>
    /// <returns>Zero.</returns>
    public double ComputeLength()
    {
        return 0.0;
    }

    /// <summary>
    /// Creates a deep copy of this point (returns this since struct is value type).
    /// </summary>
    /// <returns>A copy of this point.</returns>
    IGeometry IGeometry.Clone()
    {
        return this;
    }

    /// <summary>
    /// Returns a string representation of this point.
    /// </summary>
    /// <returns>A formatted string containing the coordinates.</returns>
    public override string ToString()
    {
        return IsEmpty ? "Point2D(Empty)" : $"Point2D({X}, {Y})";
    }
}

/// <summary>
/// Concrete line segment geometry implementing IGeometry per FR-004.
///
/// Represents a one-dimensional line segment connecting two points in a 2D coordinate system.
/// Line segments are used for boundary segments and edges between junctions.
///
/// Follows readonly record struct pattern for value-type semantics and immutability.
/// </summary>
public readonly record struct LineSegment : IGeometry
{
    /// <summary>
    /// The starting point of the line segment.
    /// </summary>
    public Point2D Start { get; }

    /// <summary>
    /// The ending point of the line segment.
    /// </summary>
    public Point2D End { get; }

    /// <summary>
    /// Gets a value indicating whether this line segment is empty (either endpoint is empty).
    /// </summary>
    public bool IsEmpty => Start.IsEmpty || End.IsEmpty;

    /// <summary>
    /// Gets the spatial dimension of this geometry (always 1 for line segments).
    /// </summary>
    public int Dimension => 1;

    /// <summary>
    /// Gets the concrete type of this geometry (always LineSegment for LineSegment).
    /// </summary>
    GeometryType IGeometry.GeometryType => GeometryType.LineSegment;

    /// <summary>
    /// Initializes a new instance of the LineSegment struct with the specified endpoints.
    /// </summary>
    /// <param name="start">The starting point.</param>
    /// <param name="end">The ending point.</param>
    public LineSegment(Point2D start, Point2D end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Initializes a new instance of the LineSegment struct with the specified coordinate endpoints.
    /// </summary>
    /// <param name="x1">The X coordinate of the starting point.</param>
    /// <param name="y1">The Y coordinate of the starting point.</param>
    /// <param name="x2">The X coordinate of the ending point.</param>
    /// <param name="y2">The Y coordinate of the ending point.</param>
    public LineSegment(double x1, double y1, double x2, double y2)
    {
        Start = new Point2D(x1, y1);
        End = new Point2D(x2, y2);
    }

    /// <summary>
    /// Gets an empty line segment.
    /// </summary>
    public static LineSegment Empty => new(Point2D.Empty, Point2D.Empty);

    /// <summary>
    /// Computes the Euclidean length of this line segment.
    /// </summary>
    /// <returns>The length of the line segment, or NaN if empty.</returns>
    public double ComputeLength()
    {
        return Start.DistanceTo(End);
    }

    /// <summary>
    /// Creates a deep copy of this line segment (returns this since struct is value type).
    /// </summary>
    /// <returns>A copy of this line segment.</returns>
    IGeometry IGeometry.Clone()
    {
        return this;
    }

    /// <summary>
    /// Returns a string representation of this line segment.
    /// </summary>
    /// <returns>A formatted string containing the start and end points.</returns>
    public override string ToString()
    {
        return IsEmpty ? "LineSegment(Empty)" : $"LineSegment({Start} -> {End})";
    }
}

/// <summary>
/// Concrete polyline geometry implementing IGeometry per FR-004.
///
/// Represents a one-dimensional polyline consisting of multiple connected line segments.
/// Polylines are used for boundary paths and multi-segment edges.
///
/// Implements sealed class pattern to ensure proper reference-type cloning semantics.
/// </summary>
public sealed class Polyline : IGeometry
{
    private readonly List<Point2D> _points;

    /// <summary>
    /// Gets the spatial dimension of this geometry (always 1 for polylines).
    /// </summary>
    public int Dimension => 1;

    /// <summary>
    /// Gets the concrete type of this geometry (always Polyline for Polyline).
    /// </summary>
    GeometryType IGeometry.GeometryType => GeometryType.Polyline;

    /// <summary>
    /// Gets a value indicating whether this polyline is empty (no points).
    /// </summary>
    public bool IsEmpty => _points.Count == 0;

    /// <summary>
    /// Gets the number of points in this polyline.
    /// </summary>
    public int PointCount => _points.Count;

    /// <summary>
    /// Gets the points in this polyline (read-only copy).
    /// </summary>
    public IReadOnlyList<Point2D> Points => _points.ToArray();

    /// <summary>
    /// Initializes a new instance of the Polyline class.
    /// </summary>
    public Polyline()
    {
        _points = new List<Point2D>();
    }

    /// <summary>
    /// Initializes a new instance of the Polyline class with the specified points.
    /// </summary>
    /// <param name="points">The points that define the polyline.</param>
    public Polyline(IEnumerable<Point2D> points)
    {
        _points = new List<Point2D>(points ?? throw new ArgumentNullException(nameof(points)));
    }

    /// <summary>
    /// Creates a new empty polyline.
    /// </summary>
    public static Polyline Empty => new Polyline();

    /// <summary>
    /// Creates a polyline from the specified coordinates.
    /// </summary>
    /// <param name="coordinates">An alternating sequence of X and Y coordinates.</param>
    /// <returns>A new polyline.</returns>
    /// <exception cref="ArgumentException">Thrown if the number of coordinates is odd.</exception>
    public static Polyline FromCoordinates(params double[] coordinates)
    {
        if (coordinates == null)
            throw new ArgumentNullException(nameof(coordinates));

        if (coordinates.Length % 2 != 0)
            throw new ArgumentException("Coordinates must have an even number of values (X1, Y1, X2, Y2, ...)", nameof(coordinates));

        var points = new List<Point2D>(coordinates.Length / 2);
        for (int i = 0; i < coordinates.Length; i += 2)
        {
            points.Add(new Point2D(coordinates[i], coordinates[i + 1]));
        }

        return new Polyline(points);
    }

    /// <summary>
    /// Adds a point to this polyline.
    /// </summary>
    /// <param name="point">The point to add.</param>
    public void AddPoint(Point2D point)
    {
        _points.Add(point);
    }

    /// <summary>
    /// Computes the total length of this polyline by summing all segment lengths.
    /// </summary>
    /// <returns>The total length, or zero if empty or single point.</returns>
    public double ComputeLength()
    {
        if (IsEmpty || _points.Count < 2)
            return 0.0;

        double totalLength = 0.0;
        for (int i = 0; i < _points.Count - 1; i++)
        {
            totalLength += _points[i].DistanceTo(_points[i + 1]);
        }

        return totalLength;
    }

    /// <summary>
    /// Creates a deep copy of this polyline.
    /// </summary>
    /// <returns>A new polyline with the same points.</returns>
    public IGeometry Clone()
    {
        return new Polyline(_points);
    }

    /// <summary>
    /// Returns a string representation of this polyline.
    /// </summary>
    /// <returns>A formatted string containing the points.</returns>
    public override string ToString()
    {
        if (IsEmpty)
            return "Polyline(Empty)";

        var pointStrings = _points.Select(p => p.ToString());
        return $"Polyline([{string.Join(", ", pointStrings)}])";
    }
}
