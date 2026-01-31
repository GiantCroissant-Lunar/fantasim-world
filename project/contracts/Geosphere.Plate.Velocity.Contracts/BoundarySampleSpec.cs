using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Specification for sampling points along a plate boundary (RFC-V2-0048 §3.1).
/// </summary>
[MessagePackObject]
[StructLayout(LayoutKind.Auto)]
public readonly record struct BoundarySampleSpec
{
    /// <summary>
    /// Number of samples to generate along the boundary. If null, <see cref="Spacing"/> must be specified.
    /// </summary>
    [Key(0)]
    public int? SampleCount { get; init; }

    /// <summary>
    /// Arc-length spacing between samples in body-frame distance units. If null, <see cref="SampleCount"/> must be specified.
    /// </summary>
    [Key(1)]
    public double? Spacing { get; init; }

    /// <summary>
    /// Sampling mode: how to distribute samples along the curve.
    /// </summary>
    [Key(2)]
    public SamplingMode Mode { get; init; }

    /// <summary>
    /// Optional: minimum distance from junction endpoints (avoids singularities).
    /// </summary>
    [Key(3)]
    public double? JunctionBufferDistance { get; init; }

    /// <summary>
    /// Optional: interpolation method for computing sample point positions.
    /// </summary>
    [Key(4)]
    public InterpolationMethod Interpolation { get; init; }
}
