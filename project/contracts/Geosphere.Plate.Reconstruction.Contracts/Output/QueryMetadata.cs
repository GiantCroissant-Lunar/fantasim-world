using MessagePack;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;

/// <summary>
/// Query metadata for reconstruction results per RFC-V2-0045 section 3.1.
/// Contains information about the query context and cache state.
/// </summary>
[MessagePackObject]
public sealed record QueryMetadata
{
    /// <summary>
    /// Version of the query contract used.
    /// </summary>
    [Key(0)]
    public required string QueryContractVersion { get; init; }

    /// <summary>
    /// Identifier of the solver implementation used.
    /// </summary>
    [Key(1)]
    public required string SolverImplementation { get; init; }

    /// <summary>
    /// Whether the result was served from cache.
    /// </summary>
    [Key(2)]
    public required bool CacheHit { get; init; }

    /// <summary>
    /// Cache key used for this query (null if caching disabled).
    /// </summary>
    [Key(3)]
    public string? CacheKey { get; init; }

    /// <summary>
    /// Hash of the topology stream at the reference tick.
    /// </summary>
    [Key(4)]
    public required string TopologyStreamHash { get; init; }

    /// <summary>
    /// Hash of the kinematics stream at the reference tick.
    /// </summary>
    [Key(5)]
    public required string KinematicsStreamHash { get; init; }

    /// <summary>
    /// The topology reference tick used for the query.
    /// </summary>
    [Key(6)]
    public required CanonicalTick TopologyReferenceTick { get; init; }

    /// <summary>
    /// The target tick requested in the query.
    /// </summary>
    [Key(7)]
    public required CanonicalTick QueryTick { get; init; }

    /// <summary>
    /// Any warnings generated during query execution.
    /// </summary>
    [Key(8)]
    public required string[] Warnings { get; init; }
}
