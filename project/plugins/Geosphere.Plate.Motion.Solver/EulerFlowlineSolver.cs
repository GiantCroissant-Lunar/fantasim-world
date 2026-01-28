using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Motion.Contracts;

namespace FantaSim.Geosphere.Plate.Motion.Solver;

/// <summary>
/// Computes flowlines using Euler integration for motion path tracing (RFC-V2-0035 §8.2).
/// </summary>
/// <remarks>
/// <para>
/// <b>Algorithm:</b> For each boundary velocity sample, extracts the seed position and
/// resolves the plate based on PlateSide (Left uses boundary.PlateIdLeft, Right uses
/// boundary.PlateIdRight). Delegates to EulerMotionPathSolver for the actual integration.
/// </para>
/// <para>
/// <b>Determinism:</b> Pure function with no I/O. Same inputs always produce identical outputs.
/// Suitable for Solver Lab corpus verification.
/// </para>
/// </remarks>
public sealed class EulerFlowlineSolver : IFlowlineSolver
{
    private readonly EulerMotionPathSolver _motionPathSolver;

    public EulerFlowlineSolver(IPlateVelocitySolver velocitySolver)
    {
        ArgumentNullException.ThrowIfNull(velocitySolver);
        _motionPathSolver = new EulerMotionPathSolver(velocitySolver);
    }

    public Flowline ComputeFlowline(
        BoundaryId boundaryId,
        BoundaryVelocitySample seed,
        PlateSide side,
        CanonicalTick startTick,
        CanonicalTick endTick,
        IntegrationDirection direction,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        MotionIntegrationSpec? spec = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(kinematics);

        // Resolve the plate based on side
        var plateId = ResolvePlateId(boundaryId, side, topology);

        // Extract seed position from the boundary velocity sample
        var seedPosition = new UnifyGeometry.Point3(
            seed.Position.X,
            seed.Position.Y,
            seed.Position.Z);

        // Compute motion path using the Euler solver
        var motionPath = _motionPathSolver.ComputeMotionPath(
            plateId,
            seedPosition,
            startTick,
            endTick,
            direction,
            topology,
            kinematics,
            spec);

        // Transform motion path into flowline
        return new Flowline(
            boundaryId,
            seed.SampleIndex,
            side,
            motionPath.StartTick,
            motionPath.EndTick,
            motionPath.Direction,
            motionPath.Samples);
    }

    public ImmutableArray<Flowline> ComputeFlowlinesForBoundary(
        BoundaryId boundaryId,
        IReadOnlyList<BoundaryVelocitySample> samples,
        PlateSide side,
        CanonicalTick startTick,
        CanonicalTick endTick,
        IntegrationDirection direction,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        MotionIntegrationSpec? spec = null)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(kinematics);

        var flowlines = new List<Flowline>(samples.Count);

        // Iterate through samples in order (by index)
        for (var i = 0; i < samples.Count; i++)
        {
            var flowline = ComputeFlowline(
                boundaryId,
                samples[i],
                side,
                startTick,
                endTick,
                direction,
                topology,
                kinematics,
                spec);

            flowlines.Add(flowline);
        }

        return flowlines.ToImmutableArray();
    }

    /// <summary>
    /// Resolves the plate ID based on the specified side of the boundary.
    /// </summary>
    private static PlateId ResolvePlateId(
        BoundaryId boundaryId,
        PlateSide side,
        IPlateTopologyStateView topology)
    {
        if (!topology.Boundaries.TryGetValue(boundaryId, out var boundary))
        {
            throw new ArgumentException(
                $"Boundary {boundaryId} not found in topology",
                nameof(boundaryId));
        }

        return side switch
        {
            PlateSide.Left => boundary.PlateIdLeft,
            PlateSide.Right => boundary.PlateIdRight,
            _ => throw new ArgumentException($"Unknown plate side: {side}", nameof(side))
        };
    }
}
