using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Result of trajectory integration.
/// Contains the complete path with metadata.
/// </summary>
public sealed record Trajectory
{
    /// <summary>
    /// All samples along the trajectory, ordered by tick.
    /// </summary>
    public required ImmutableArray<TrajectorySample> Samples { get; init; }

    /// <summary>
    /// Total accumulated error estimate.
    /// </summary>
    public required double TotalAccumulatedError { get; init; }

    /// <summary>
    /// Number of plate boundary crossings encountered.
    /// </summary>
    public required int BoundaryCrossingCount { get; init; }

    /// <summary>
    /// Complete provenance for the integration.
    /// </summary>
    public required TrajectoryProvenance Provenance { get; init; }

    /// <summary>
    /// Integration statistics for diagnostics.
    /// </summary>
    public required IntegrationStatistics Statistics { get; init; }
}

/// <summary>
/// Single sample point along a trajectory.
/// </summary>
public sealed record TrajectorySample
{
    /// <summary>
    /// Time of this sample.
    /// </summary>
    public required CanonicalTick Tick { get; init; }

    /// <summary>
    /// Position in the reference frame (from policy).
    /// </summary>
    public required Point3 Position { get; init; }

    /// <summary>
    /// Plate containing this point at this tick.
    /// </summary>
    public required PlateId PlateId { get; init; }

    /// <summary>
    /// Velocity at this position/tick.
    /// </summary>
    public required Vector3d Velocity { get; init; }

    /// <summary>
    /// Accumulated error estimate at this sample.
    /// </summary>
    public required double AccumulatedError { get; init; }

    /// <summary>
    /// Provenance for this specific sample.
    /// </summary>
    public required SampleProvenance Provenance { get; init; }

    /// <summary>
    /// If this sample is at a boundary crossing, the crossed boundary.
    /// </summary>
    public BoundaryId? CrossedBoundary { get; init; }
}

/// <summary>
/// Provenance for the entire trajectory.
/// </summary>
public sealed record TrajectoryProvenance
{
    // Define properties as needed, potentially logic version or method used
    public string IntegratorVersion { get; init; } = "1.0";
}

/// <summary>
/// Provenance for a single sample.
/// </summary>
public sealed record SampleProvenance
{
    // E.g. which segment was used, similar to ReconstructionProvenance
    public ReconstructionProvenance ReconstructionInfo { get; init; }
}

/// <summary>
/// Statistics about the integration process.
/// </summary>
public sealed record IntegrationStatistics
{
    public int SampleCount { get; init; }
    public double ComputationTimeMs { get; init; }
}
