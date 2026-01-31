using System.Collections.Generic;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Extension point for custom trajectory sampling behavior.
/// Motion Paths and Flowlines implement this differently.
/// </summary>
public interface ITrajectorySampler
{
    /// <summary>
    /// Determines if an additional sample should be taken at the current state.
    /// </summary>
    bool ShouldSample(TrajectoryIntegrationState state);

    /// <summary>
    /// Computes any additional metadata for the sample.
    /// </summary>
    Dictionary<string, object> ComputeMetadata(TrajectoryIntegrationState state);
}

/// <summary>
/// Current state during trajectory integration.
/// Passed to sampler for decision-making.
/// </summary>
public sealed record TrajectoryIntegrationState
{
    public required Point3 CurrentPosition { get; init; }
    public required Vector3d CurrentVelocity { get; init; }
    public required PlateId CurrentPlateId { get; init; }
    public required CanonicalTick CurrentTick { get; init; }
    public required double AccumulatedError { get; init; }
    public required int StepCount { get; init; }
    public TrajectorySample? PreviousSample { get; init; }
}
