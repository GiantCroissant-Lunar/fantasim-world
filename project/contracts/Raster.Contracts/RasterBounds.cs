using MessagePack;
using UnifyGeometry;

namespace FantaSim.Raster.Contracts;

/// <summary>
/// Geographic bounds of a raster dataset.
/// </summary>
[MessagePackObject]
public readonly record struct RasterBounds(
    [property: Key(0)] double MinLongitude,
    [property: Key(1)] double MaxLongitude,
    [property: Key(2)] double MinLatitude,
    [property: Key(3)] double MaxLatitude
)
{
    /// <summary>
    /// Width of the bounds in degrees.
    /// </summary>
    [IgnoreMember]
    public double Width => MaxLongitude - MinLongitude;

    /// <summary>
    /// Height of the bounds in degrees.
    /// </summary>
    [IgnoreMember]
    public double Height => MaxLatitude - MinLatitude;

    /// <summary>
    /// Checks if a point is within these bounds.
    /// </summary>
    public bool Contains(double longitude, double latitude)
        => longitude >= MinLongitude && longitude <= MaxLongitude
        && latitude >= MinLatitude && latitude <= MaxLatitude;

    /// <summary>
    /// Checks if a point is within these bounds.
    /// </summary>
    public bool Contains(Point2 point)
        => Contains(point.X, point.Y);

    /// <summary>
    /// Global bounds (-180 to 180, -90 to 90).
    /// </summary>
    public static RasterBounds Global => new(-180.0, 180.0, -90.0, 90.0);
}
