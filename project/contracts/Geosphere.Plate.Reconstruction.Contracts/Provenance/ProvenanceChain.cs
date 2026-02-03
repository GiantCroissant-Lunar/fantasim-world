using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Provenance;

/// <summary>
/// Complete provenance chain for reconstruction query results per RFC-V2-0045 section 5.1.
/// Tracks the full lineage of data sources used to compute a reconstruction result.
/// </summary>
[MessagePackObject]
public sealed record ProvenanceChain
{
    /// <summary>
    /// Feature identifiers that contributed to this result.
    /// </summary>
    [Key(0)]
    public required FeatureId[] SourceFeatureIds { get; init; }

    /// <summary>
    /// Boundary identifiers that contributed to this result.
    /// </summary>
    [Key(1)]
    public required BoundaryId[] SourceBoundaryIds { get; init; }

    /// <summary>
    /// Junction identifiers that contributed to this result.
    /// </summary>
    [Key(2)]
    public required JunctionId[] SourceJunctionIds { get; init; }

    /// <summary>
    /// Plate assignment provenance tracking how features were assigned to plates.
    /// </summary>
    [Key(3)]
    public required PlateAssignmentProvenance PlateAssignment { get; init; }

    /// <summary>
    /// Kinematics provenance tracking the motion model data used.
    /// </summary>
    [Key(4)]
    public required KinematicsProvenance Kinematics { get; init; }

    /// <summary>
    /// Stream provenance containing topology and kinematics stream hashes.
    /// </summary>
    [Key(5)]
    public required StreamProvenance Stream { get; init; }

    /// <summary>
    /// Query metadata at the time of computation.
    /// </summary>
    [Key(6)]
    public required QueryProvenanceMetadata QueryMetadata { get; init; }
}

/// <summary>
/// Provenance for plate assignment decisions.
/// </summary>
[MessagePackObject]
public sealed record PlateAssignmentProvenance
{
    /// <summary>
    /// The plate identifier assigned during reconstruction.
    /// </summary>
    [Key(0)]
    public required PlateId AssignedPlateId { get; init; }

    /// <summary>
    /// The method used for plate assignment.
    /// </summary>
    [Key(1)]
    public required PlateAssignmentMethod Method { get; init; }

    /// <summary>
    /// Confidence score of the assignment (0.0 to 1.0).
    /// </summary>
    [Key(2)]
    public required double Confidence { get; init; }
}

/// <summary>
/// Method used for plate assignment.
/// </summary>
public enum PlateAssignmentMethod
{
    /// <summary>
    /// Assignment based on point-in-polygon containment.
    /// </summary>
    Containment = 0,

    /// <summary>
    /// Assignment based on nearest boundary distance.
    /// </summary>
    NearestBoundary = 1,

    /// <summary>
    /// Assignment explicitly specified in input.
    /// </summary>
    Explicit = 2,

    /// <summary>
    /// Assignment inherited from parent feature.
    /// </summary>
    Inherited = 3
}

/// <summary>
/// Provenance for kinematics data used in reconstruction.
/// </summary>
[MessagePackObject]
public sealed record KinematicsProvenance
{
    /// <summary>
    /// The reference frame used for reconstruction.
    /// </summary>
    [Key(0)]
    public required ReferenceFrameId ReferenceFrame { get; init; }

    /// <summary>
    /// Motion segment identifiers used in the reconstruction.
    /// </summary>
    [Key(1)]
    public required Guid[] MotionSegmentIds { get; init; }

    /// <summary>
    /// The interpolation method used for rotation computation.
    /// </summary>
    [Key(2)]
    public required string InterpolationMethod { get; init; }
}

/// <summary>
/// Stream provenance containing hashes for topology and kinematics truth streams.
/// </summary>
[MessagePackObject]
public sealed record StreamProvenance
{
    /// <summary>
    /// Hash of the topology stream at the reference tick.
    /// </summary>
    [Key(0)]
    public required string TopologyStreamHash { get; init; }

    /// <summary>
    /// Hash of the kinematics stream at the reference tick.
    /// </summary>
    [Key(1)]
    public required string KinematicsStreamHash { get; init; }

    /// <summary>
    /// The topology reference tick used.
    /// </summary>
    [Key(2)]
    public required CanonicalTick TopologyReferenceTick { get; init; }

    /// <summary>
    /// The kinematics reference tick used.
    /// </summary>
    [Key(3)]
    public required CanonicalTick KinematicsReferenceTick { get; init; }
}

/// <summary>
/// Query-time metadata captured in provenance.
/// </summary>
[MessagePackObject]
public sealed record QueryProvenanceMetadata
{
    /// <summary>
    /// The query tick requested.
    /// </summary>
    [Key(0)]
    public required CanonicalTick QueryTick { get; init; }

    /// <summary>
    /// Timestamp when the query was executed (UTC ticks).
    /// </summary>
    [Key(1)]
    public required long ExecutionTimestampUtc { get; init; }

    /// <summary>
    /// Solver implementation version string.
    /// </summary>
    [Key(2)]
    public required string SolverVersion { get; init; }
}
