using System;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Spatial bounding box for filtering.
/// </summary>
[MessagePackObject]
public readonly record struct BoundingBox
{
    /// <summary>
    /// Gets the minimum longitude in degrees.
    /// </summary>
    [Key(0)]
    public required double MinLongitude { get; init; }

    /// <summary>
    /// Gets the maximum longitude in degrees.
    /// </summary>
    [Key(1)]
    public required double MaxLongitude { get; init; }

    /// <summary>
    /// Gets the minimum latitude in degrees.
    /// </summary>
    [Key(2)]
    public required double MinLatitude { get; init; }

    /// <summary>
    /// Gets the maximum latitude in degrees.
    /// </summary>
    [Key(3)]
    public required double MaxLatitude { get; init; }

    /// <summary>
    /// Creates a bounding box from center point and radius.
    /// </summary>
    public static BoundingBox FromCenter(double centerLon, double centerLat, double radiusDegrees)
    {
        return new BoundingBox
        {
            MinLongitude = centerLon - radiusDegrees,
            MaxLongitude = centerLon + radiusDegrees,
            MinLatitude = Math.Max(-90.0, centerLat - radiusDegrees),
            MaxLatitude = Math.Min(90.0, centerLat + radiusDegrees)
        };
    }

    /// <summary>
    /// Determines if a point is within this bounding box.
    /// </summary>
    public bool Contains(double longitude, double latitude)
    {
        return longitude >= MinLongitude && longitude <= MaxLongitude &&
               latitude >= MinLatitude && latitude <= MaxLatitude;
    }
}
