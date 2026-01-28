using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;

namespace FantaSim.Geosphere.Plate.Velocity.Solver;

/// <summary>
/// Computes plate velocities from finite rotation kinematics.
/// </summary>
/// <remarks>
/// <para>
/// <b>Algorithm:</b>
/// <list type="number">
/// <item>Get rotation R(t) at tick t</item>
/// <item>Get rotation R(t + dt) at tick t + dt (dt = 1 tick for determinism)</item>
/// <item>Compute ΔR = R(t+dt) × R(t)⁻¹</item>
/// <item>Extract angular velocity ω from ΔR using axis-angle conversion</item>
/// <item>Linear velocity at point p is v = ω × p</item>
/// </list>
/// </para>
/// <para>
/// <b>Determinism:</b> Uses dt = 1 tick (constant) to ensure deterministic results
/// across platforms and corpora. Same inputs always produce identical outputs.
/// </para>
/// <para>
/// <b>Fallback:</b> Returns zero velocity when kinematics data is missing,
/// matching the reconstruction solver policy (RFC-V2-0024).
/// </para>
/// </remarks>
public sealed class FiniteRotationPlateVelocitySolver : IPlateVelocitySolver
{
    /// <summary>
    /// Delta ticks used for finite difference velocity computation.
    /// Fixed at 1 tick for determinism.
    /// </summary>
    public const int DeltaTicks = 1;

    public Velocity3d GetAbsoluteVelocity(
        IPlateKinematicsStateView kinematics,
        PlateId plateId,
        Vector3d point,
        CanonicalTick tick)
    {
        ArgumentNullException.ThrowIfNull(kinematics);

        var omega = GetAngularVelocity(kinematics, plateId, tick);
        return omega.GetLinearVelocityAt(point.X, point.Y, point.Z);
    }

    public Velocity3d GetRelativeVelocity(
        IPlateKinematicsStateView kinematics,
        PlateId plateIdA,
        PlateId plateIdB,
        Vector3d point,
        CanonicalTick tick)
    {
        ArgumentNullException.ThrowIfNull(kinematics);

        var velocityA = GetAbsoluteVelocity(kinematics, plateIdA, point, tick);
        var velocityB = GetAbsoluteVelocity(kinematics, plateIdB, point, tick);
        return velocityA - velocityB;
    }

    public AngularVelocity3d GetAngularVelocity(
        IPlateKinematicsStateView kinematics,
        PlateId plateId,
        CanonicalTick tick)
    {
        ArgumentNullException.ThrowIfNull(kinematics);

        // Get rotations at t and t + dt
        if (!kinematics.TryGetRotation(plateId, tick, out var r0))
            return AngularVelocity3d.Zero;

        var tickPlusDt = new CanonicalTick(tick.Value + DeltaTicks);
        if (!kinematics.TryGetRotation(plateId, tickPlusDt, out var r1))
            return AngularVelocity3d.Zero;

        // Compute delta rotation: ΔR = R(t+dt) × R(t)⁻¹
        var r0Inv = Inverse(r0);
        var deltaR = Multiply(r1, r0Inv);

        // Extract axis-angle from delta rotation
        var (axis, angle) = ToAxisAngle(deltaR);

        // Angular velocity = axis × (angle / dt)
        var rate = angle / DeltaTicks;
        return new AngularVelocity3d(axis.X * rate, axis.Y * rate, axis.Z * rate);
    }

    /// <summary>
    /// Converts a quaternion to axis-angle representation.
    /// </summary>
    private static (Vector3d Axis, double Angle) ToAxisAngle(Quaterniond q)
    {
        // Normalize the quaternion
        var norm = Math.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W);
        if (norm < double.Epsilon)
            return (Vector3d.Zero, 0);

        var qn = new Quaterniond(q.X / norm, q.Y / norm, q.Z / norm, q.W / norm);

        // Clamp W to avoid NaN from acos
        var w = Math.Clamp(qn.W, -1.0, 1.0);
        var angle = 2.0 * Math.Acos(w);

        // Handle zero rotation case
        if (angle < double.Epsilon)
            return (Vector3d.Zero, 0);

        // Compute axis
        var sinHalfAngle = Math.Sin(angle / 2.0);
        if (Math.Abs(sinHalfAngle) < double.Epsilon)
            return (Vector3d.Zero, 0);

        var axis = new Vector3d(
            qn.X / sinHalfAngle,
            qn.Y / sinHalfAngle,
            qn.Z / sinHalfAngle);

        return (axis, angle);
    }

    private static Quaterniond Inverse(Quaterniond q)
    {
        var norm = (q.X * q.X) + (q.Y * q.Y) + (q.Z * q.Z) + (q.W * q.W);
        if (norm == 0d)
            return Quaterniond.Identity;

        var c = Conjugate(q);
        return new Quaterniond(c.X / norm, c.Y / norm, c.Z / norm, c.W / norm);
    }

    private static Quaterniond Conjugate(Quaterniond q)
        => new(-q.X, -q.Y, -q.Z, q.W);

    private static Quaterniond Multiply(Quaterniond a, Quaterniond b)
    {
        return new Quaterniond(
            (a.W * b.X) + (a.X * b.W) + (a.Y * b.Z) - (a.Z * b.Y),
            (a.W * b.Y) - (a.X * b.Z) + (a.Y * b.W) + (a.Z * b.X),
            (a.W * b.Z) + (a.X * b.Y) - (a.Y * b.X) + (a.Z * b.W),
            (a.W * b.W) - (a.X * b.X) - (a.Y * b.Y) - (a.Z * b.Z));
    }
}
