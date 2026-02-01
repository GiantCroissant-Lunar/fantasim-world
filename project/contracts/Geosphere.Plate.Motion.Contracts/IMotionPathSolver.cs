using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;

namespace FantaSim.Geosphere.Plate.Motion.Contracts;

/// <summary>
/// Solver for computing motion paths (RFC-V2-0049 §3.1).
/// </summary>
public interface IMotionPathSolver
{
    /// <summary>
    /// Computes the motion path of a point attached to a plate.
    /// </summary>
    /// <param name="plateId">The plate the point is attached to.</param>
    /// <param name="startPoint">The initial position (body frame at startTick).</param>
    /// <param name="startTick">Integration start time.</param>
    /// <param name="endTick">Integration end time.</param>
    /// <param name="direction">Direction of integration (forward or backward).</param>
    /// <param name="topology">The topology state view.</param>
    /// <param name="kinematics">The kinematics state view.</param>
    /// <param name="stepPolicy">Integration step policy.</param>
    /// <param name="frameId">Reference frame for output.</param>
    /// <returns>The computed motion path.</returns>
    MotionPath ComputeMotionPath(
        PlateId plateId,
        Point3 startPoint,
        CanonicalTick startTick,
        CanonicalTick endTick,
        IntegrationDirection direction,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        StepPolicy stepPolicy,
        ReferenceFrameId frameId);
}
