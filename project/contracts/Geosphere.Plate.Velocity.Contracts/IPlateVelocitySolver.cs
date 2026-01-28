using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Velocity.Contracts;

/// <summary>
/// Computes plate velocities from kinematics truth.
/// </summary>
/// <remarks>
/// <para>
/// <b>RFC-V2-0033:</b> Velocity products are derived outputs computed from kinematics truth.
/// They are recomputable, emit no truth events, and may be cached.
/// </para>
/// <para>
/// <b>Determinism:</b> Same inputs MUST produce identical outputs.
/// Suitable for Solver Lab corpus verification.
/// </para>
/// <para>
/// <b>Fallback:</b> If kinematics data is missing for a plate at the requested tick,
/// implementations MUST return zero velocity (not throw). This matches the reconstruction
/// solver fallback policy (RFC-V2-0024).
/// </para>
/// <para>
/// <b>Point frame semantics:</b> The input point is expected to be in body frame at the
/// target tick (i.e., already reconstructed to that tick). This keeps the velocity solver
/// independent from the reconstruction solver.
/// </para>
/// </remarks>
public interface IPlateVelocitySolver
{
    /// <summary>
    /// Computes the absolute velocity of a point anchored to a plate.
    /// </summary>
    /// <param name="kinematics">The kinematics state view to query rotations from.</param>
    /// <param name="plateId">The plate the point is anchored to.</param>
    /// <param name="point">The position on the sphere (body frame at target tick).</param>
    /// <param name="tick">The target simulation time.</param>
    /// <returns>Velocity vector in body-frame units per tick.</returns>
    /// <remarks>
    /// Returns <see cref="Velocity3d.Zero"/> if kinematics data is missing.
    /// </remarks>
    Velocity3d GetAbsoluteVelocity(
        IPlateKinematicsStateView kinematics,
        PlateId plateId,
        Vector3d point,
        CanonicalTick tick);

    /// <summary>
    /// Computes the velocity of a point on plate A relative to plate B.
    /// </summary>
    /// <param name="kinematics">The kinematics state view to query rotations from.</param>
    /// <param name="plateIdA">The plate the point is anchored to.</param>
    /// <param name="plateIdB">The reference plate.</param>
    /// <param name="point">The position on the sphere (body frame at target tick).</param>
    /// <param name="tick">The target simulation time.</param>
    /// <returns>Relative velocity vector (vA - vB at the same point).</returns>
    /// <remarks>
    /// Returns <see cref="Velocity3d.Zero"/> if kinematics data is missing for either plate.
    /// </remarks>
    Velocity3d GetRelativeVelocity(
        IPlateKinematicsStateView kinematics,
        PlateId plateIdA,
        PlateId plateIdB,
        Vector3d point,
        CanonicalTick tick);

    /// <summary>
    /// Computes the angular velocity of a plate.
    /// </summary>
    /// <param name="kinematics">The kinematics state view to query rotations from.</param>
    /// <param name="plateId">The plate.</param>
    /// <param name="tick">The target simulation time.</param>
    /// <returns>Angular velocity (axis × rate in radians per tick).</returns>
    /// <remarks>
    /// Returns <see cref="AngularVelocity3d.Zero"/> if kinematics data is missing.
    /// </remarks>
    AngularVelocity3d GetAngularVelocity(
        IPlateKinematicsStateView kinematics,
        PlateId plateId,
        CanonicalTick tick);
}
