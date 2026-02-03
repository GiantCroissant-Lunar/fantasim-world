using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Service.Contracts;

/// <summary>
/// Captures the complete provenance of a reconstruction query result per RFC-V2-0045 Section 5.
/// </summary>
/// <remarks>
/// The ProvenanceChain provides full traceability of how a reconstruction result was computed,
/// including all source data, models, and solver implementations used. This enables:
/// - Result reproducibility
/// - Cache invalidation tracking
/// - Audit trails for scientific applications
/// - Determinism verification
///
/// Per RFC-V2-0045 Section 5, the provenance chain must include all specified fields
/// for strict provenance validation to succeed.
/// </remarks>
[MessagePackObject]
public sealed record ProvenanceChain
{
    /// <summary>
    /// Gets the source feature identifiers that contributed to this result.
    /// </summary>
    [Key(0)]
    public required IReadOnlyList<FeatureId> SourceFeatureIds { get; init; }

    /// <summary>
    /// Gets the source boundary identifiers that contributed to this result.
    /// </summary>
    [Key(1)]
    public required IReadOnlyList<BoundaryId> SourceBoundaryIds { get; init; }

    /// <summary>
    /// Gets the source junction identifiers that contributed to this result.
    /// </summary>
    [Key(2)]
    public required IReadOnlyList<JunctionId> SourceJunctionIds { get; init; }

    /// <summary>
    /// Gets the plate identifier for the assigned plate (if applicable).
    /// </summary>
    [Key(3)]
    public PlateId? PlateId { get; init; }

    /// <summary>
    /// Gets the kinematics model identifier used for rotations.
    /// </summary>
    [Key(4)]
    public required FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies.ModelId KinematicsModelId { get; init; }

    /// <summary>
    /// Gets the kinematics model version (for cache invalidation).
    /// </summary>
    [Key(5)]
    public required int KinematicsModelVersion { get; init; }

    /// <summary>
    /// Gets the rotation segments applied during reconstruction.
    /// </summary>
    [Key(6)]
    public required IReadOnlyList<RotationSegmentRef> RotationSegments { get; init; }

    /// <summary>
    /// Gets the topology stream hash for data integrity verification.
    /// </summary>
    [Key(7)]
    public required byte[] TopologyStreamHash { get; init; }

    /// <summary>
    /// Gets the topology reference tick (the tick at which topology was resolved).
    /// </summary>
    [Key(8)]
    public required CanonicalTick TopologyReferenceTick { get; init; }

    /// <summary>
    /// Gets the query tick (the target reconstruction time).
    /// </summary>
    [Key(9)]
    public required CanonicalTick QueryTick { get; init; }

    /// <summary>
    /// Gets the query contract version for API compatibility.
    /// </summary>
    [Key(10)]
    public required string QueryContractVersion { get; init; }

    /// <summary>
    /// Gets the solver implementation identifier.
    /// </summary>
    [Key(11)]
    public required string SolverImplementation { get; init; }

    /// <summary>
    /// Gets the reference frame used for this reconstruction.
    /// </summary>
    [Key(12)]
    public FantaSim.Geosphere.Plate.Kinematics.Contracts.ReferenceFrameId? ReferenceFrame { get; init; }

    /// <summary>
    /// Gets the provenance of the frame transform applied.
    /// </summary>
    [Key(13)]
    public FrameTransformProvenance? FrameTransform { get; init; }

    /// <summary>
    /// Validates that this provenance chain meets strictness requirements.
    /// </summary>
    /// <param name="strictness">The strictness level to validate against.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public bool Validate(ProvenanceStrictness strictness)
    {
        if (strictness == ProvenanceStrictness.Permissive)
            return true;

        // All required fields must be present
        if (SourceFeatureIds == null || SourceBoundaryIds == null || SourceJunctionIds == null)
            return false;

        if (KinematicsModelId.IsEmpty)
            return false;

        if (RotationSegments == null || RotationSegments.Count == 0)
            return false;

        if (TopologyStreamHash == null || TopologyStreamHash.Length == 0)
            return false;

        if (QueryTick.Value < 0)
            return false;

        if (string.IsNullOrEmpty(QueryContractVersion))
            return false;

        if (string.IsNullOrEmpty(SolverImplementation))
            return false;

        if (strictness == ProvenanceStrictness.Strict)
        {
            // Strict mode requires additional validation
            if (SourceFeatureIds.Count == 0 && SourceBoundaryIds.Count == 0 && SourceJunctionIds.Count == 0)
                return false;

            // Verify topology stream hash length (SHA-256 = 32 bytes)
            if (TopologyStreamHash.Length != 32)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Creates an empty provenance chain for testing scenarios.
    /// </summary>
    public static ProvenanceChain Empty => new()
    {
        SourceFeatureIds = Array.Empty<FeatureId>(),
        SourceBoundaryIds = Array.Empty<BoundaryId>(),
        SourceJunctionIds = Array.Empty<JunctionId>(),
        KinematicsModelId = default,
        KinematicsModelVersion = 0,
        RotationSegments = Array.Empty<RotationSegmentRef>(),
        TopologyStreamHash = Array.Empty<byte>(),
        TopologyReferenceTick = CanonicalTick.Genesis,
        QueryTick = CanonicalTick.Genesis,
        QueryContractVersion = string.Empty,
        SolverImplementation = string.Empty
    };
}
