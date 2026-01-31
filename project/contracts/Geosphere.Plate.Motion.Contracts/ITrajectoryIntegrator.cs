using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Integrates a trajectory through the plate velocity field.
/// Shared contract for both Motion Paths and Flowlines.
/// </summary>
public interface ITrajectoryIntegrator
{
    /// <summary>
    /// Integrates a trajectory from start tick to end tick.
    /// </summary>
    /// <param name="context">Integration context including seed, policy, and services</param>
    /// <returns>Integrated trajectory with samples at each step</returns>
    Trajectory Integrate(TrajectoryIntegrationContext context);
}

/// <summary>
/// Input context for trajectory integration.
/// Contains all parameters needed for the integration.
/// </summary>
public sealed record TrajectoryIntegrationContext
{
    /// <summary>
    /// Starting position on the body surface.
    /// </summary>
    public required Point3 SeedPoint { get; init; }

    /// <summary>
    /// Starting plate assignment (may change during integration).
    /// </summary>
    public required PlateId SeedPlateId { get; init; }

    /// <summary>
    /// Time range for integration.
    /// </summary>
    public required CanonicalTick StartTick { get; init; }
    public required CanonicalTick EndTick { get; init; }

    /// <summary>
    /// Reconstruction policy controlling frame, tolerance, integration step.
    /// </summary>
    public required StepPolicy Policy { get; init; }

    /// <summary>
    /// Topology state view for plate assignment queries.
    /// </summary>
    public required IPlateTopologyStateView Topology { get; init; }

    /// <summary>
    /// Kinematics state view for velocity queries.
    /// </summary>
    public required IPlateKinematicsStateView Kinematics { get; init; }

    /// <summary>
    /// Optional: Custom sampling callback for specialized behaviors.
    /// If null, uses standard sampling.
    /// </summary>
    public ITrajectorySampler? CustomSampler { get; init; }

    /// <summary>
    /// Direction of integration (forward or backward in time).
    /// </summary>
    public IntegrationDirection Direction { get; init; } = IntegrationDirection.Forward;
}
