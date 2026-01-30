using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;

namespace FantaSim.World.Plates;

#region Reconstruct Query

/// <summary>
/// Result of a Reconstruct query per RFC-V2-0045 Section 3.1.
/// </summary>
/// <remarks>
/// Contains the reconstructed features, complete provenance chain, and query metadata.
/// Results are stably sorted by SourceFeatureId.Value ascending per RFC requirement.
/// </remarks>
[MessagePackObject]
public sealed record ReconstructResult
{
    /// <summary>
    /// Gets the reconstructed features.
    /// </summary>
    /// <remarks>
    /// Per RFC-V2-0045: Results are stably sorted by SourceFeatureId.Value ascending.
    /// </remarks>
    [Key(0)]
    public required IReadOnlyList<ReconstructedFeature> Features { get; init; }

    /// <summary>
    /// Gets the complete provenance chain for this result.
    /// </summary>
    [Key(1)]
    public required ProvenanceChain Provenance { get; init; }

    /// <summary>
    /// Gets the query execution metadata.
    /// </summary>
    [Key(2)]
    public required QueryMetadata Metadata { get; init; }

    /// <summary>
    /// Gets the continuation cursor for pagination (if more results available).
    /// </summary>
    [Key(3)]
    public string? ContinuationCursor { get; init; }

    /// <summary>
    /// Gets a value indicating whether there are more results.
    /// </summary>
    [IgnoreMember]
    public bool HasMore => !string.IsNullOrEmpty(ContinuationCursor);

    /// <summary>
    /// Gets the total count of features (may exceed Features.Count if paginated).
    /// </summary>
    [Key(4)]
    public int? TotalCount { get; init; }

    /// <summary>
    /// Validates that this result meets RFC-V2-0045 requirements.
    /// </summary>
    /// <param name="strictness">The strictness level for validation.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public bool Validate(ProvenanceStrictness strictness)
    {
        if (Features == null)
            return false;

        if (!Provenance.Validate(strictness))
            return false;

        // Verify stable sorting by SourceFeatureId.Value
        if (Features.Count > 1)
        {
            for (int i = 1; i < Features.Count; i++)
            {
                var prev = Features[i - 1].SourceFeatureId.Value;
                var curr = Features[i].SourceFeatureId.Value;
                if (string.Compare(prev.ToString("D"), curr.ToString("D"), StringComparison.Ordinal) > 0)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates an empty result for error scenarios.
    /// </summary>
    public static ReconstructResult Empty(ProvenanceChain? provenance = null, QueryMetadata? metadata = null) => new()
    {
        Features = Array.Empty<ReconstructedFeature>(),
        Provenance = provenance ?? ProvenanceChain.Empty,
        Metadata = metadata ?? QueryMetadata.ForCacheHit("empty", "none")
    };
}

/// <summary>
/// Represents a reconstructed feature with its provenance per RFC-V2-0045.
/// </summary>
[MessagePackObject]
public sealed record ReconstructedFeature
{
    /// <summary>
    /// Gets the source feature identifier.
    /// </summary>
    [Key(0)]
    public required FeatureId SourceFeatureId { get; init; }

    /// <summary>
    /// Gets the plate that this feature was reconstructed to.
    /// </summary>
    [Key(1)]
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// Gets the reconstructed geometry in the target reference frame.
    /// </summary>
    [Key(2)]
    public required IGeometry Geometry { get; init; }

    /// <summary>
    /// Gets the original geometry before reconstruction (for reference).
    /// </summary>
    [Key(3)]
    public IGeometry? OriginalGeometry { get; init; }

    /// <summary>
    /// Gets the rotation applied to reconstruct this feature.
    /// </summary>
    [Key(4)]
    public ReconstructionRotation? AppliedRotation { get; init; }

    /// <summary>
    /// Gets the confidence level of this reconstruction.
    /// </summary>
    [Key(5)]
    public ReconstructionConfidence Confidence { get; init; } = ReconstructionConfidence.High;

    /// <summary>
    /// Gets metadata specific to this feature reconstruction.
    /// </summary>
    [Key(6)]
    public FeatureReconstructionMetadata? FeatureMetadata { get; init; }
}

/// <summary>
/// Describes the rotation applied during feature reconstruction.
/// </summary>
[MessagePackObject]
public readonly record struct ReconstructionRotation
{
    /// <summary>
    /// Gets the Euler pole latitude in degrees.
    /// </summary>
    [Key(0)]
    public required double EulerPoleLatitude { get; init; }

    /// <summary>
    /// Gets the Euler pole longitude in degrees.
    /// </summary>
    [Key(1)]
    public required double EulerPoleLongitude { get; init; }

    /// <summary>
    /// Gets the rotation angle in degrees.
    /// </summary>
    [Key(2)]
    public required double RotationAngleDegrees { get; init; }

    /// <summary>
    /// Gets the rotation segment reference.
    /// </summary>
    [Key(3)]
    public required RotationSegmentRef SegmentRef { get; init; }
}

/// <summary>
/// Confidence level for feature reconstruction.
/// </summary>
public enum ReconstructionConfidence
{
    /// <summary>
    /// High confidence reconstruction with minimal uncertainty.
    /// </summary>
    High = 0,

    /// <summary>
    /// Medium confidence with some uncertainty in rotation parameters.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Low confidence due to extrapolation or limited data.
    /// </summary>
    Low = 2,

    /// <summary>
    /// Reconstruction involved interpolation between known time steps.
    /// </summary>
    Interpolated = 3
}

/// <summary>
/// Metadata for individual feature reconstruction.
/// </summary>
[MessagePackObject]
public sealed record FeatureReconstructionMetadata
{
    /// <summary>
    /// Gets the source tick (when the feature was originally defined).
    /// </summary>
    [Key(0)]
    public required CanonicalTick SourceTick { get; init; }

    /// <summary>
    /// Gets the target tick (reconstruction time).
    /// </summary>
    [Key(1)]
    public required CanonicalTick TargetTick { get; init; }

    /// <summary>
    /// Gets the reconstruction method used.
    /// </summary>
    [Key(2)]
    public required string Method { get; init; }

    /// <summary>
    /// Gets any warnings specific to this feature.
    /// </summary>
    [Key(3)]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

#endregion

#region QueryPlateId

/// <summary>
/// Result of a QueryPlateId operation per RFC-V2-0045 Section 3.2.
/// </summary>
[MessagePackObject]
public sealed record PlateAssignmentResult
{
    /// <summary>
    /// Gets the assigned plate identifier.
    /// </summary>
    [Key(0)]
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// Gets the confidence level of this assignment.
    /// </summary>
    [Key(1)]
    public required PlateAssignmentConfidence Confidence { get; init; }

    /// <summary>
    /// Gets the list of candidate plates (for uncertain assignments).
    /// </summary>
    [Key(2)]
    public IReadOnlyList<CandidatePlate> CandidatePlates { get; init; } = Array.Empty<CandidatePlate>();

    /// <summary>
    /// Gets the distance to the nearest plate boundary in degrees.
    /// </summary>
    [Key(3)]
    public double? DistanceToBoundaryDegrees { get; init; }

    /// <summary>
    /// Gets the complete provenance chain.
    /// </summary>
    [Key(4)]
    public required ProvenanceChain Provenance { get; init; }

    /// <summary>
    /// Gets the query execution metadata.
    /// </summary>
    [Key(5)]
    public required QueryMetadata Metadata { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is an unambiguous assignment.
    /// </summary>
    [IgnoreMember]
    public bool IsUnambiguous => Confidence == PlateAssignmentConfidence.Definite;
}

/// <summary>
/// Represents a candidate plate for uncertain assignments.
/// </summary>
[MessagePackObject]
public readonly record struct CandidatePlate
{
    /// <summary>
    /// Gets the plate identifier.
    /// </summary>
    [Key(0)]
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// Gets the probability weight for this candidate (0.0-1.0).
    /// </summary>
    [Key(1)]
    public required double Probability { get; init; }

    /// <summary>
    /// Gets the distance from the query point to this plate's boundary.
    /// </summary>
    [Key(2)]
    public double DistanceToBoundaryDegrees { get; init; }
}

#endregion

#region QueryVelocity

/// <summary>
/// Result of a QueryVelocity operation per RFC-V2-0045 Section 3.3.
/// </summary>
[MessagePackObject]
public sealed record VelocityResult
{
    /// <summary>
    /// Gets the total velocity vector at the query point.
    /// </summary>
    [Key(0)]
    public required Velocity3d TotalVelocity { get; init; }

    /// <summary>
    /// Gets the velocity decomposition (plate vs. boundary contributions).
    /// </summary>
    [Key(1)]
    public required VelocityDecomposition Decomposition { get; init; }

    /// <summary>
    /// Gets the plate identifier at the query point.
    /// </summary>
    [Key(2)]
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// Gets the complete provenance chain.
    /// </summary>
    [Key(3)]
    public required ProvenanceChain Provenance { get; init; }

    /// <summary>
    /// Gets the query execution metadata.
    /// </summary>
    [Key(4)]
    public required QueryMetadata Metadata { get; init; }

    /// <summary>
    /// Gets the velocity magnitude in mm/year.
    /// </summary>
    [IgnoreMember]
    public double MagnitudeMmYr => TotalVelocity.MagnitudeMmYr;
}

/// <summary>
/// Represents a 3D velocity vector.
/// </summary>
[MessagePackObject]
public readonly record struct Velocity3d
{
    /// <summary>
    /// Gets the east-west velocity component (positive east) in mm/year.
    /// </summary>
    [Key(0)]
    public required double EastMmYr { get; init; }

    /// <summary>
    /// Gets the north-south velocity component (positive north) in mm/year.
    /// </summary>
    [Key(1)]
    public required double NorthMmYr { get; init; }

    /// <summary>
    /// Gets the vertical velocity component (positive up) in mm/year.
    /// </summary>
    [Key(2)]
    public required double VerticalMmYr { get; init; }

    /// <summary>
    /// Gets the velocity magnitude in mm/year.
    /// </summary>
    [IgnoreMember]
    public double MagnitudeMmYr => Math.Sqrt(
        EastMmYr * EastMmYr +
        NorthMmYr * NorthMmYr +
        VerticalMmYr * VerticalMmYr);

    /// <summary>
    /// Gets the azimuth (direction) in degrees clockwise from north.
    /// </summary>
    [IgnoreMember]
    public double AzimuthDegrees => Math.Atan2(EastMmYr, NorthMmYr) * (180.0 / Math.PI);

    /// <summary>
    /// Creates a Velocity3d from Cartesian components.
    /// </summary>
    public static Velocity3d FromCartesian(double vx, double vy, double vz) => new()
    {
        EastMmYr = vx,
        NorthMmYr = vy,
        VerticalMmYr = vz
    };

    /// <summary>
    /// Creates a Velocity3d from magnitude and azimuth (horizontal only).
    /// </summary>
    public static Velocity3d FromHorizontal(double magnitudeMmYr, double azimuthDegrees) => new()
    {
        EastMmYr = magnitudeMmYr * Math.Sin(azimuthDegrees * Math.PI / 180.0),
        NorthMmYr = magnitudeMmYr * Math.Cos(azimuthDegrees * Math.PI / 180.0),
        VerticalMmYr = 0.0
    };
}

/// <summary>
/// Decomposes velocity into contributing components.
/// </summary>
[MessagePackObject]
public sealed record VelocityDecomposition
{
    /// <summary>
    /// Gets the rigid plate rotation component.
    /// </summary>
    [Key(0)]
    public required Velocity3d PlateRotationComponent { get; init; }

    /// <summary>
    /// Gets the boundary interaction component (if near boundary).
    /// </summary>
    [Key(1)]
    public Velocity3d? BoundaryInteractionComponent { get; init; }

    /// <summary>
    /// Gets the internal deformation component (if any).
    /// </summary>
    [Key(2)]
    public Velocity3d? InternalDeformationComponent { get; init; }

    /// <summary>
    /// Gets the method used for velocity calculation.
    /// </summary>
    [Key(3)]
    public required VelocityMethod Method { get; init; }

    /// <summary>
    /// Gets the confidence level of this decomposition.
    /// </summary>
    [Key(4)]
    public VelocityConfidence Confidence { get; init; } = VelocityConfidence.High;

    /// <summary>
    /// Gets boundary proximity information (if near boundary).
    /// </summary>
    [Key(5)]
    public BoundaryProximity? BoundaryProximity { get; init; }
}

/// <summary>
/// Method used for velocity calculation.
/// </summary>
public enum VelocityMethod
{
    /// <summary>
    /// Pure rigid plate rotation using Euler pole.
    /// </summary>
    RigidRotation = 0,

    /// <summary>
    /// Velocity interpolated from boundary samples.
    /// </summary>
    BoundaryInterpolation = 1,

    /// <summary>
    /// Velocity from finite difference of reconstructed positions.
    /// </summary>
    FiniteDifference = 2,

    /// <summary>
    /// Velocity from direct kinematic model evaluation.
    /// </summary>
    DirectKinematic = 3
}

/// <summary>
/// Confidence level for velocity calculation.
/// </summary>
public enum VelocityConfidence
{
    /// <summary>
    /// High confidence with minimal uncertainty.
    /// </summary>
    High = 0,

    /// <summary>
    /// Medium confidence with some uncertainty.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Low confidence due to boundary proximity or data limitations.
    /// </summary>
    Low = 2,

    /// <summary>
    /// Extrapolated velocity beyond known time range.
    /// </summary>
    Extrapolated = 3
}

/// <summary>
/// Information about boundary proximity for velocity calculations.
/// </summary>
[MessagePackObject]
public readonly record struct BoundaryProximity
{
    /// <summary>
    /// Gets the nearest boundary identifier.
    /// </summary>
    [Key(0)]
    public required BoundaryId BoundaryId { get; init; }

    /// <summary>
    /// Gets the distance to the boundary in degrees.
    /// </summary>
    [Key(1)]
    public required double DistanceDegrees { get; init; }

    /// <summary>
    /// Gets the boundary type.
    /// </summary>
    [Key(2)]
    public required string BoundaryType { get; init; }

    /// <summary>
    /// Gets the adjacent plate across the boundary.
    /// </summary>
    [Key(3)]
    public PlateId? AdjacentPlateId { get; init; }
}

#endregion
