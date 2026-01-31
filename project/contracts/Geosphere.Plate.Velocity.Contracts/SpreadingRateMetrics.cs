using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Spreading rate metrics for ridge boundaries (RFC-V2-0048 §5.3).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct SpreadingRateMetrics(
    [property: Key(0)] double FullRate,
    [property: Key(1)] double HalfRate,
    [property: Key(2)] double Asymmetry,
    [property: Key(3)] double Obliquity,
    [property: Key(4)] double AlongStrikeVariation,
    [property: Key(5)] double MeanDivergenceRate,
    [property: Key(6)] int DivergentSampleCount
);
