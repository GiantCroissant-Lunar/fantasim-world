using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using MessagePack;

namespace FantaSim.World.Plates;

/// <summary>
/// Defines the policy for plate reconstruction operations per RFC-V2-0045 Section 4.1.
/// </summary>
/// <remarks>
/// The ReconstructionPolicy record encapsulates all configurable parameters that affect
/// reconstruction behavior. It is designed to be immutable, hashable, and serializable
/// for use in cache keys and distributed scenarios.
///
/// Per RFC-V2-0045 Section 4.2.1: The Frame property MUST be included in every cache key.
/// </remarks>
[MessagePackObject]
public sealed record ReconstructionPolicy
{
    /// <summary>
    /// Gets the reference frame for reconstruction.
    /// </summary>
    /// <remarks>
    /// Per RFC-V2-0045 Section 4.2.1: This field MUST be included in every cache key.
    /// </remarks>
    [Key(0)]
    public required ReferenceFrameId Frame { get; init; }

    /// <summary>
    /// Gets the kinematics model identifier for rotation calculations.
    /// </summary>
    [Key(1)]
    public required ModelId KinematicsModel { get; init; }

    /// <summary>
    /// Gets the tolerance policy for point-in-polygon and boundary operations.
    /// </summary>
    [Key(2)]
    public required TolerancePolicy PartitionTolerance { get; init; }

    /// <summary>
    /// Gets the optional boundary sampling specification for velocity calculations.
    /// </summary>
    [Key(3)]
    public BoundarySampleSpec? BoundarySampling { get; init; }

    /// <summary>
    /// Gets the optional integration step policy for motion path calculations.
    /// </summary>
    [Key(4)]
    public StepPolicy? IntegrationPolicy { get; init; }

    /// <summary>
    /// Gets the provenance strictness level for validation.
    /// </summary>
    [Key(5)]
    public ProvenanceStrictness Strictness { get; init; } = ProvenanceStrictness.Strict;


}



/// <summary>
/// Uniquely identifies a kinematics model for rotation calculations.
/// </summary>
[MessagePackObject]
public readonly record struct ModelId
{
    private readonly Guid _value;

    [SerializationConstructor]
    public ModelId(Guid value)
    {
        _value = value;
    }

    [Key(0)]
    public Guid Value => _value;

    [IgnoreMember]
    public bool IsEmpty => _value == Guid.Empty;

    public static ModelId NewId() => new(Guid.NewGuid());

    public static ModelId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ModelId cannot be null or whitespace.", nameof(value));
        return new ModelId(Guid.Parse(value));
    }

    public override string ToString() => _value.ToString("D");
}

/// <summary>
/// Specifies boundary sampling parameters for velocity calculations.
/// </summary>
/// <remarks>
/// Per RFC-V2-0045 Section 4.1: Boundary sampling affects velocity interpolation
/// near plate boundaries where multiple plates may contribute.
/// </remarks>
[MessagePackObject]
public sealed record BoundarySampleSpec
{
    /// <summary>
    /// Gets the number of sample points along boundary segments.
    /// </summary>
    [Key(0)]
    public required int SampleCount { get; init; }

    /// <summary>
    /// Gets the maximum distance from boundary to include samples.
    /// </summary>
    [Key(1)]
    public required double MaxDistanceDegrees { get; init; }

    /// <summary>
    /// Gets the interpolation method for boundary-adjacent points.
    /// </summary>
    [Key(2)]
    public BoundaryInterpolationMode Interpolation { get; init; } = BoundaryInterpolationMode.Linear;

    public override int GetHashCode()
    {
        return HashCode.Combine(SampleCount, MaxDistanceDegrees, Interpolation);
    }
}

/// <summary>
/// Defines interpolation modes for boundary-adjacent velocity calculations.
/// </summary>
public enum BoundaryInterpolationMode
{
    /// <summary>
    /// Simple linear interpolation between adjacent plate velocities.
    /// </summary>
    Linear = 0,

    /// <summary>
    /// Smooth step interpolation to avoid discontinuities.
    /// </summary>
    SmoothStep = 1,

    /// <summary>
    /// Cosine-weighted interpolation for natural transitions.
    /// </summary>
    Cosine = 2
}
