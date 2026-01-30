using System.Runtime.InteropServices;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Sampling specification for boundary velocity analysis.
/// </summary>
/// <remarks>
/// <para>
/// Controls how many samples are taken along a boundary and the sampling strategy.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public record struct BoundarySamplingSpec(
    int SampleCount,
    SamplingMode Mode,
    bool IncludeEndpoints = true
);
