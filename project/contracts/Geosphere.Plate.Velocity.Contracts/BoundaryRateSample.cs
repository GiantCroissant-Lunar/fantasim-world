using System.Runtime.InteropServices;
using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Complete rate analysis at a single boundary sample point (RFC-V2-0048 §4.6).
/// </summary>
/// <remarks>
/// Extends <see cref="BoundaryVelocitySample"/> with additional analytics including
/// vertical rate, aggregate metrics, uncertainty bounds, and provenance tracking.
/// </remarks>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct BoundaryRateSample(
    [property: Key(0)] Vector3d Position,
    [property: Key(1)] Velocity3d RelativeVelocity,
    [property: Key(2)] Vector3d Tangent,
    [property: Key(3)] Vector3d Normal,
    [property: Key(4)] Vector3d Vertical,
    [property: Key(5)] double TangentialRate,
    [property: Key(6)] double NormalRate,
    [property: Key(7)] double? VerticalRate,
    [property: Key(8)] double RelativeSpeed,
    [property: Key(9)] double RelativeAzimuth,
    [property: Key(10)] double ObliquityAngle,
    [property: Key(11)] RateUncertainty Uncertainty,
    [property: Key(12)] double ArcLength,
    [property: Key(13)] int SampleIndex
);
