using System.Runtime.InteropServices;
using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Complete rate analysis at a single boundary sample point (RFC-V2-0048 §4.6).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct BoundaryRateSample(
    [property: Key(0)] Vector3d Position,
    [property: Key(1)] double ArcLength,
    [property: Key(2)] Velocity3d RelativeVelocity,
    [property: Key(3)] Vector3d Tangent,
    [property: Key(4)] Vector3d Normal,
    [property: Key(5)] Vector3d Vertical,
    [property: Key(6)] double NormalRate,
    [property: Key(7)] double TangentialRate,
    [property: Key(8)] double? VerticalRate,
    [property: Key(9)] double RelativeSpeed,
    [property: Key(10)] double RelativeAzimuth,
    [property: Key(11)] double ObliquityAngle,
    [property: Key(12)] RateUncertainty Uncertainty,
    [property: Key(13)] SampleProvenance Provenance
);
