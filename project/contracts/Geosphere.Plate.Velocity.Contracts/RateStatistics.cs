using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Aggregate rate statistics for a boundary (RFC-V2-0048 §5.6).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct RateStatistics(
    [property: Key(0)] double MinNormalRate,
    [property: Key(1)] double MaxNormalRate,
    [property: Key(2)] double MeanNormalRate,
    [property: Key(3)] double MinTangentialRate,
    [property: Key(4)] double MaxTangentialRate,
    [property: Key(5)] double MeanTangentialRate,
    [property: Key(6)] double MaxRelativeSpeed,
    [property: Key(7)] double MeanRelativeSpeed,
    [property: Key(8)] int SampleCount
);
