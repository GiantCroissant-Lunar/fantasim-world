using System;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Point in 3D space (used for QueryPlateId and QueryVelocity).
/// </summary>
[MessagePackObject]
public readonly record struct Point3
{
    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    [Key(0)]
    public required double X { get; init; }

    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    [Key(1)]
    public required double Y { get; init; }

    /// <summary>
    /// Gets the Z coordinate.
    /// </summary>
    [Key(2)]
    public required double Z { get; init; }

    /// <summary>
    /// Creates a Point3 from longitude and latitude (on unit sphere).
    /// </summary>
    public static Point3 FromLonLat(double longitude, double latitude)
    {
        var lonRad = longitude * Math.PI / 180.0;
        var latRad = latitude * Math.PI / 180.0;
        var cosLat = Math.Cos(latRad);

        return new Point3
        {
            X = cosLat * Math.Cos(lonRad),
            Y = cosLat * Math.Sin(lonRad),
            Z = Math.Sin(latRad)
        };
    }

    /// <summary>
    /// Converts this point to longitude and latitude in degrees.
    /// </summary>
    public (double Longitude, double Latitude) ToLonLat()
    {
        var lat = Math.Asin(Z) * 180.0 / Math.PI;
        var lon = Math.Atan2(Y, X) * 180.0 / Math.PI;
        return (lon, lat);
    }
}
