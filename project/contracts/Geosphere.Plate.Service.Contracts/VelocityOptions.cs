using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Options for QueryVelocity operations per RFC-V2-0045 Section 3.3.
/// </summary>
[MessagePackObject]
public sealed record VelocityOptions
{
    /// <summary>
    /// Gets the reference frame for velocity calculation.
    /// </summary>
    [Key(0)]
    public ReferenceFrameId? Frame { get; init; }

    /// <summary>
    /// Gets the model identifier for kinematics.
    /// </summary>
    [Key(1)]
    public ModelId? ModelId { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include velocity decomposition.
    /// </summary>
    [Key(2)]
    public bool IncludeDecomposition { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to include boundary proximity info.
    /// </summary>
    [Key(3)]
    public bool IncludeBoundaryInfo { get; init; }

    /// <summary>
    /// Gets the time delta for finite difference calculations (in ticks).
    /// </summary>
    [Key(4)]
    public long? FiniteDifferenceDeltaTicks { get; init; }

    /// <summary>
    /// Gets the boundary sample specification (overrides policy default).
    /// </summary>
    [Key(5)]
    public BoundarySampleSpec? BoundarySamplingOverride { get; init; }

    /// <summary>
    /// Gets the interpolation method for boundary-adjacent velocities.
    /// </summary>
    [Key(6)]
    public InterpolationMethod InterpolationMode { get; init; } = InterpolationMethod.Linear;

    /// <summary>
    /// Gets the default options instance.
    /// </summary>
    public static VelocityOptions Default { get; } = new();

    /// <summary>
    /// Creates options for boundary-aware velocity calculations.
    /// </summary>
    public static VelocityOptions WithBoundaryInfo() => new()
    {
        IncludeBoundaryInfo = true,
        IncludeDecomposition = true
    };

    /// <summary>
    /// Creates options for finite difference velocity calculation.
    /// </summary>
    public static VelocityOptions ForFiniteDifference(long deltaTicks = 1) => new()
    {
        FiniteDifferenceDeltaTicks = deltaTicks
    };
}
