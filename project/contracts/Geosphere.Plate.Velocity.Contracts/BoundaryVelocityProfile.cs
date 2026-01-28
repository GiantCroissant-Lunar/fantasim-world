using System.Collections.Immutable;
using System.Runtime.InteropServices;
using MessagePack;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Per-boundary velocity profile with aggregated statistics (RFC-V2-0034 §10.2).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct BoundaryVelocityProfile(
    [property: Key(0)] BoundaryId BoundaryId,
    [property: Key(1)] int SampleCount,
    [property: Key(2)] double MinNormalRate,
    [property: Key(3)] double MaxNormalRate,
    [property: Key(4)] double MeanNormalRate,
    [property: Key(5)] double MeanSlipRate,
    [property: Key(6)] int MinSampleIndex,
    [property: Key(7)] int MaxSampleIndex
);
