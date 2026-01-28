using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Velocity.Contracts;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Solver for computing flowlines from boundary samples (RFC-V2-0035 §8.2).
/// </summary>
public interface IFlowlineSolver
{
    /// <summary>
    /// Computes a flowline from a boundary sample.
    /// The seed position is taken directly from the sample to ensure consistency.
    /// </summary>
    /// <param name="boundaryId">The source boundary for seeding.</param>
    /// <param name="seed">The boundary velocity sample to seed from.</param>
    /// <param name="side">Which plate side to trace (Left or Right).</param>
    /// <param name="startTick">Integration start time.</param>
    /// <param name="endTick">Integration end time.</param>
    /// <param name="direction">Direction of integration (forward or backward).</param>
    /// <param name="topology">The topology state view.</param>
    /// <param name="kinematics">The kinematics state view.</param>
    /// <param name="spec">Optional integration specification (defaults used if null).</param>
    /// <returns>The computed flowline.</returns>
    Flowline ComputeFlowline(
        BoundaryId boundaryId,
        BoundaryVelocitySample seed,
        PlateSide side,
        CanonicalTick startTick,
        CanonicalTick endTick,
        IntegrationDirection direction,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        MotionIntegrationSpec? spec = null);

    /// <summary>
    /// Computes flowlines for all samples in a boundary profile.
    /// </summary>
    /// <param name="boundaryId">The source boundary for seeding.</param>
    /// <param name="samples">The boundary velocity samples to seed from.</param>
    /// <param name="side">Which plate side to trace (Left or Right).</param>
    /// <param name="startTick">Integration start time.</param>
    /// <param name="endTick">Integration end time.</param>
    /// <param name="direction">Direction of integration (forward or backward).</param>
    /// <param name="topology">The topology state view.</param>
    /// <param name="kinematics">The kinematics state view.</param>
    /// <param name="spec">Optional integration specification (defaults used if null).</param>
    /// <returns>An immutable array of computed flowlines.</returns>
    ImmutableArray<Flowline> ComputeFlowlinesForBoundary(
        BoundaryId boundaryId,
        IReadOnlyList<BoundaryVelocitySample> samples,
        PlateSide side,
        CanonicalTick startTick,
        CanonicalTick endTick,
        IntegrationDirection direction,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        MotionIntegrationSpec? spec = null);
}
