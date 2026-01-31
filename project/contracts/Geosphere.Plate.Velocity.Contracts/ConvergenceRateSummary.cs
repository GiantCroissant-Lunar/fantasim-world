using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Convergence rate summary for convergent boundaries (RFC-V2-0048 §5.1).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct ConvergenceRateSummary(
    [property: Key(0)] double MaxConvergenceRate,
    [property: Key(1)] double MeanConvergenceRate,
    [property: Key(2)] double TotalConvergentLength,
    [property: Key(3)] int ConvergentSampleCount,
    [property: Key(4)] double? MaxConvergenceUncertainty
);
