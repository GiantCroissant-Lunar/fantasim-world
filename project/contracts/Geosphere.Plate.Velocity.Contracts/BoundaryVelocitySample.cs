using System.Runtime.InteropServices;
using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Per-sample velocity data for boundary analysis (RFC-V2-0034 §10.1).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct BoundaryVelocitySample(
    [property: Key(0)] Vector3d Position,
    [property: Key(1)] Velocity3d RelativeVelocity,
    [property: Key(2)] Vector3d Tangent,
    [property: Key(3)] Vector3d Normal,
    [property: Key(4)] double TangentialRate,
    [property: Key(5)] double NormalRate,
    [property: Key(6)] int SampleIndex
);
