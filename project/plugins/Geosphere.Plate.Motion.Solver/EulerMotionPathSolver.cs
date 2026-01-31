using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Motion.Contracts;

namespace FantaSim.Geosphere.Plate.Motion.Solver;

/// <summary>
/// Computes motion paths using Euler integration (RFC-V2-0049 §3).
/// Refactored to use shared TrajectoryIntegrator (RFC-V2-0049a).
/// </summary>
public sealed class EulerMotionPathSolver : IMotionPathSolver
{
    private readonly ITrajectoryIntegrator _integrator;

    // Maintain constructor compatibility for existing tests/DI
    public EulerMotionPathSolver(IPlateVelocitySolver velocitySolver)
    {
        ArgumentNullException.ThrowIfNull(velocitySolver);
        _integrator = new TrajectoryIntegrator(velocitySolver);
    }

    // Optional: Allow injecting integrator directly if needed
    public EulerMotionPathSolver(ITrajectoryIntegrator integrator)
    {
        _integrator = integrator ?? throw new ArgumentNullException(nameof(integrator));
    }

    public MotionPath ComputeMotionPath(
        PlateId plateId,
        Point3 startPoint,
        CanonicalTick startTick,
        CanonicalTick endTick,
        IntegrationDirection direction,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        StepPolicy stepPolicy,
        ReferenceFrameId frameId)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(kinematics);
        ArgumentNullException.ThrowIfNull(stepPolicy);

        var context = new TrajectoryIntegrationContext
        {
            SeedPoint = startPoint,
            SeedPlateId = plateId,
            StartTick = startTick,
            EndTick = endTick,
            Policy = stepPolicy,
            Topology = topology,
            Kinematics = kinematics,
            Direction = direction,
            CustomSampler = null
        };

        var trajectory = _integrator.Integrate(context);

        // Convert Trajectory to MotionPath
        var samples = trajectory.Samples.Select(s => new MotionPathSample(
            s.Tick,
            s.Position,
            s.PlateId,
            s.Velocity,
            s.Provenance.ReconstructionInfo,
            s.AccumulatedError
        ));

        return new MotionPath(
            plateId,
            startTick,
            endTick,
            direction,
            frameId,
            samples.ToImmutableArray());
    }
}
