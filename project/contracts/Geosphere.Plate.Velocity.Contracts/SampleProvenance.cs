using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Provenance information for a boundary sample point (RFC-V2-0048 §4.6).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct SampleProvenance(
    [property: Key(0)] int SampleIndex,
    [property: Key(1)] int SegmentIndex,
    [property: Key(2)] double SegmentT
);
