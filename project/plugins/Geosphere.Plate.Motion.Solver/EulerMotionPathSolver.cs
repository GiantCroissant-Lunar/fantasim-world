using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Motion.Contracts;

namespace FantaSim.Geosphere.Plate.Motion.Solver;

/// <summary>
/// Computes motion paths using Euler integration (RFC-V2-0035 §9.2).
/// </summary>
/// <remarks>
/// <para>
/// <b>Algorithm:</b>
/// <code>
/// p(t + Δt) = NormalizeToBodySurface( p(t) + v(p,t) * Δt )
/// </code>
/// where Δt = StepTicks and v(p,t) is queried via IPlateVelocitySolver.GetAbsoluteVelocity().
/// </para>
/// <para>
/// <b>Determinism:</b> Pure function with no I/O. Same inputs always produce identical outputs.
/// Suitable for Solver Lab corpus verification.
/// </para>
/// <para>
/// <b>Fallback:</b> Returns zero velocity when kinematics data is missing,
/// allowing integration to continue (the point stays stationary for that step).
/// </para>
/// </remarks>
public sealed class EulerMotionPathSolver : IMotionPathSolver
{
    private readonly IPlateVelocitySolver _velocitySolver;

    public EulerMotionPathSolver(IPlateVelocitySolver velocitySolver)
    {
        _velocitySolver = velocitySolver ?? throw new ArgumentNullException(nameof(velocitySolver));
    }

    public MotionPath ComputeMotionPath(
        PlateId plateId,
        Point3 startPoint,
        CanonicalTick startTick,
        CanonicalTick endTick,
        IntegrationDirection direction,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        MotionIntegrationSpec? spec = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(kinematics);

        // Use default spec if not provided (use .Default, not new() due to struct semantics)
        var integrationSpec = spec ?? MotionIntegrationSpec.Default;

        // Build samples list
        var samples = new List<MotionPathSample>();
        var p = startPoint;
        var t = startTick;
        var step = 0;

        while (!ReachedEnd(t, endTick, direction) && step < integrationSpec.MaxSteps)
        {
            // Query velocity at current position and time
            var positionVector = new Vector3d(p.X, p.Y, p.Z);
            var v = _velocitySolver.GetAbsoluteVelocity(kinematics, plateId, positionVector, t);

            // Record sample
            samples.Add(new MotionPathSample(t, p, v, step));

            // Compute dt based on direction
            var dt = direction == IntegrationDirection.Forward
                ? integrationSpec.StepTicks
                : -integrationSpec.StepTicks;

            // Euler step: p' = p + v * dt
            var newPositionVector = positionVector + new Vector3d(v.X * dt, v.Y * dt, v.Z * dt);

            // Project back onto body surface (unit sphere normalization)
            p = NormalizeToBodySurface(newPositionVector);

            // Advance time
            t = new CanonicalTick(t.Value + dt);
            step++;
        }

        return new MotionPath(plateId, startTick, endTick, direction, samples.ToImmutableArray());
    }

    /// <summary>
    /// Determines if integration has reached the end tick based on direction.
    /// </summary>
    private static bool ReachedEnd(CanonicalTick currentTick, CanonicalTick endTick, IntegrationDirection direction)
    {
        return direction == IntegrationDirection.Forward
            ? currentTick.Value >= endTick.Value
            : currentTick.Value <= endTick.Value;
    }

    /// <summary>
    /// Projects a point onto the body surface (unit sphere).
    /// </summary>
    /// <remarks>
    /// For the MVP, we assume a unit sphere body frame. The point is normalized to unit length.
    /// Future extensions may support ellipsoid or custom surface functions.
    /// </remarks>
    private static Point3 NormalizeToBodySurface(Vector3d point)
    {
        var length = point.Length();
        if (length < double.Epsilon)
        {
            // If point is at origin, return a default unit vector
            return new Point3(1, 0, 0);
        }

        var normalized = point / length;
        return new Point3(normalized.X, normalized.Y, normalized.Z);
    }
}
