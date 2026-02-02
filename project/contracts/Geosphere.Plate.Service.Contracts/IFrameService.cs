using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Service for computing reference frame transforms.
/// </summary>
public interface IFrameService
{
    /// <summary>
    /// Computes the transform from one reference frame to another.
    /// </summary>
    /// <param name="fromFrame">The source reference frame.</param>
    /// <param name="toFrame">The target reference frame.</param>
    /// <param name="tick">The time at which to compute the transform.</param>
    /// <param name="modelId">The kinematics model ID to use.</param>
    /// <param name="kinematics">The kinematics state view (source of truth).</param>
    /// <param name="topology">The topology state view (for plate enumeration/weights).</param>
    /// <returns>The computed frame transform result.</returns>
    FrameTransformResult GetFrameTransform(
        FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId fromFrame,
        FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId toFrame,
        CanonicalTick tick,
        FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies.ModelId modelId,
        IPlateKinematicsStateView kinematics,
        IPlateTopologyStateView topology);

    /// <summary>
    /// Validates a custom frame definition for cycles and other issues.
    /// </summary>
    /// <param name="definition">The frame definition to validate.</param>
    void ValidateFrameDefinition(FrameDefinition definition);
}

/// <summary>
/// Result of a frame transformation computation.
/// </summary>
[MessagePackObject]
public sealed record FrameTransformResult
{
    /// <summary>
    /// Gets the computed finite rotation transform.
    /// </summary>
    [Key(0)]
    public required FiniteRotation Transform { get; init; }

    /// <summary>
    /// Gets the provenance information for this transform.
    /// </summary>
    [Key(1)]
    public required FrameTransformProvenance Provenance { get; init; }

    /// <summary>
    /// Gets a value indicating whether the transform is valid.
    /// </summary>
    [Key(2)]
    public required TransformValidity Validity { get; init; }

    /// <summary>
    /// Identity transform result for convenience.
    /// </summary>
    public static readonly FrameTransformResult Identity = new()
    {
        Transform = FiniteRotation.Identity,
        Provenance = new FrameTransformProvenance
        {
            FromFrame = MantleFrame.Instance,
            ToFrame = MantleFrame.Instance,
            EvaluationChain = Array.Empty<FrameChainLink>(),
            KinematicsModelVersion = "identity"
        },
        Validity = TransformValidity.Valid
    };
}

/// <summary>
/// Provenance information for a frame transform.
/// </summary>
[MessagePackObject]
public sealed record FrameTransformProvenance
{
    [Key(0)]
    public required FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId FromFrame { get; init; }

    [Key(1)]
    public required FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId ToFrame { get; init; }

    [Key(2)]
    public required IReadOnlyList<FrameChainLink> EvaluationChain { get; init; }

    [Key(3)]
    public required string KinematicsModelVersion { get; init; }
}

/// <summary>
/// validity status of a frame transform.
/// </summary>
public enum TransformValidity
{
    Valid,
    Interpolated,
    Extrapolated,
    Invalid
}
