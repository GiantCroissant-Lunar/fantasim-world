using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.World.Plates;

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
    public required ModelId KinematicsModelId { get; init; }

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
    public ReferenceFrameId? ReferenceFrame { get; init; }

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
        if (strictness == ProvenanceStrictness.Disabled)
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

/// <summary>
/// Reference to a rotation segment used in reconstruction.
/// </summary>
[MessagePackObject]
public readonly record struct RotationSegmentRef
{
    /// <summary>
    /// Gets the plate identifier this rotation applies to.
    /// </summary>
    [Key(0)]
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// Gets the start tick of the rotation segment.
    /// </summary>
    [Key(1)]
    public required CanonicalTick StartTick { get; init; }

    /// <summary>
    /// Gets the end tick of the rotation segment.
    /// </summary>
    [Key(2)]
    public required CanonicalTick EndTick { get; init; }

    /// <summary>
    /// Gets the rotation segment version for tracking updates.
    /// </summary>
    [Key(3)]
    public required int SegmentVersion { get; init; }

    /// <summary>
    /// Gets the Euler pole hash for this segment (for integrity verification).
    /// </summary>
    [Key(4)]
    public required byte[] EulerPoleHash { get; init; }
}

/// <summary>
/// Metadata associated with a reconstruction query.
/// </summary>
[MessagePackObject]
public sealed record QueryMetadata
{
    /// <summary>
    /// Gets the query execution timestamp (UTC).
    /// </summary>
    [Key(0)]
    public required DateTimeOffset ExecutedAt { get; init; }

    /// <summary>
    /// Gets the query execution duration.
    /// </summary>
    [Key(1)]
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the cache hit status.
    /// </summary>
    [Key(2)]
    public bool CacheHit { get; init; }

    /// <summary>
    /// Gets the cache key used (if applicable).
    /// </summary>
    [Key(3)]
    public string? CacheKey { get; init; }

    /// <summary>
    /// Gets any warnings generated during query execution.
    /// </summary>
    [Key(4)]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the solver version identifier.
    /// </summary>
    [Key(5)]
    public required string SolverVersion { get; init; }

    /// <summary>
    /// Creates metadata for a cache hit scenario.
    /// </summary>
    public static QueryMetadata ForCacheHit(string cacheKey, string solverVersion) => new()
    {
        ExecutedAt = DateTimeOffset.UtcNow,
        Duration = TimeSpan.Zero,
        CacheHit = true,
        CacheKey = cacheKey,
        SolverVersion = solverVersion
    };

    /// <summary>
    /// Creates metadata for a computed result.
    /// </summary>
    public static QueryMetadata ForComputed(TimeSpan duration, string solverVersion, IEnumerable<string>? warnings = null) => new()
    {
        ExecutedAt = DateTimeOffset.UtcNow,
        Duration = duration,
        CacheHit = false,
        SolverVersion = solverVersion,
        Warnings = warnings?.ToArray() ?? Array.Empty<string>()
    };
}
