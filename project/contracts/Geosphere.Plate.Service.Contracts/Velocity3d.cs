using System;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Represents a 3D velocity vector.
/// </summary>
[MessagePackObject]
public readonly record struct Velocity3d
{
    /// <summary>
    /// Gets the east-west velocity component (positive east) in mm/year.
    /// </summary>
    [Key(0)]
    public required double EastMmYr { get; init; }

    /// <summary>
    /// Gets the north-south velocity component (positive north) in mm/year.
    /// </summary>
    [Key(1)]
    public required double NorthMmYr { get; init; }

    /// <summary>
    /// Gets the vertical velocity component (positive up) in mm/year.
    /// </summary>
    [Key(2)]
    public required double VerticalMmYr { get; init; }

    /// <summary>
    /// Gets the velocity magnitude in mm/year.
    /// </summary>
    [IgnoreMember]
    public double MagnitudeMmYr => Math.Sqrt(
        EastMmYr * EastMmYr +
        NorthMmYr * NorthMmYr +
        VerticalMmYr * VerticalMmYr);

    /// <summary>
    /// Gets the azimuth (direction) in degrees clockwise from north.
    /// </summary>
    [IgnoreMember]
    public double AzimuthDegrees => Math.Atan2(EastMmYr, NorthMmYr) * (180.0 / Math.PI);

    /// <summary>
    /// Creates a Velocity3d from Cartesian components.
    /// </summary>
    public static Velocity3d FromCartesian(double vx, double vy, double vz) => new()
    {
        EastMmYr = vx,
        NorthMmYr = vy,
        VerticalMmYr = vz
    };

    /// <summary>
    /// Creates a Velocity3d from magnitude and azimuth (horizontal only).
    /// </summary>
    public static Velocity3d FromHorizontal(double magnitudeMmYr, double azimuthDegrees) => new()
    {
        EastMmYr = magnitudeMmYr * Math.Sin(azimuthDegrees * Math.PI / 180.0),
        NorthMmYr = magnitudeMmYr * Math.Cos(azimuthDegrees * Math.PI / 180.0),
        VerticalMmYr = 0.0
    };
}
