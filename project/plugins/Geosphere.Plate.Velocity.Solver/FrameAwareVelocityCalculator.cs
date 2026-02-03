using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;

namespace FantaSim.Geosphere.Plate.Velocity.Solver;

/// <summary>
/// Computes frame-aware velocity decomposition per RFC-V2-0046 Section 5.2.
/// </summary>
/// <remarks>
/// <para>
/// <b>RFC-V2-0046 Section 5.2:</b> Velocity computations MUST respect the query's reference frame.
/// The velocity relative to a frame is the difference between absolute plate velocity and frame velocity.
/// </para>
/// <para>
/// <b>Algorithm:</b>
/// <list type="number">
/// <item>Compute absolute angular velocity of the plate</item>
/// <item>Compute angular velocity of the reference frame</item>
/// <item>Subtract frame angular velocity from plate angular velocity</item>
/// <item>Compute rigid velocity at point from the relative angular velocity</item>
/// </list>
/// </para>
/// <para>
/// <b>Determinism:</b> Same inputs always produce identical outputs.
/// </para>
/// </remarks>
public sealed class FrameAwareVelocityCalculator
{
    private readonly IPlateVelocitySolver _velocitySolver;

    /// <summary>
    /// Creates a new frame-aware velocity calculator.
    /// </summary>
    /// <param name="velocitySolver">The plate velocity solver for angular velocity computation.</param>
    public FrameAwareVelocityCalculator(IPlateVelocitySolver velocitySolver)
    {
        _velocitySolver = velocitySolver ?? throw new ArgumentNullException(nameof(velocitySolver));
    }

    /// <summary>
    /// Computes velocity decomposition in a specified reference frame.
    /// </summary>
    /// <param name="point">The position on the sphere (body frame at target tick).</param>
    /// <param name="plateId">The plate the point is anchored to.</param>
    /// <param name="tick">The target simulation time.</param>
    /// <param name="frame">The reference frame for velocity computation.</param>
    /// <param name="kinematics">The kinematics state view to query rotations from.</param>
    /// <returns>
    /// Velocity decomposition relative to the specified frame, including rigid rotation component,
    /// magnitude, and azimuth.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For MantleFrame: Returns absolute velocity (frame angular velocity is the area-weighted
    /// net rotation which this implementation treats as zero for simplicity).
    /// </para>
    /// <para>
    /// For PlateAnchor: Returns velocity relative to the anchor plate.
    /// </para>
    /// <para>
    /// For AbsoluteFrame: Returns velocity adjusted for True Polar Wander (if applicable).
    /// </para>
    /// <para>
    /// For CustomFrame: Evaluates the frame chain to compute frame angular velocity.
    /// </para>
    /// </remarks>
    public VelocityDecomposition ComputeVelocityInFrame(
        Vector3d point,
        PlateId plateId,
        CanonicalTick tick,
        ReferenceFrameId frame,
        IPlateKinematicsStateView kinematics)
    {
        ArgumentNullException.ThrowIfNull(kinematics);
        ArgumentNullException.ThrowIfNull(frame);

        // Step 1: Get absolute angular velocity of the plate
        var absoluteAngularVel = _velocitySolver.GetAngularVelocity(kinematics, plateId, tick);

        // Step 2: Get angular velocity of the reference frame
        var frameAngularVel = GetFrameAngularVelocity(frame, tick, kinematics);

        // Step 3: Compute relative angular velocity (plate minus frame)
        var relativeAngularVel = absoluteAngularVel - frameAngularVel;

        // Step 4: Compute rigid velocity at point from relative angular velocity
        var rigidComponent = ComputeRigidVelocity(point, relativeAngularVel);

        // The total velocity in this frame equals the rigid component (no deformation model)
        var totalVelocity = rigidComponent;

        // Compute magnitude and azimuth
        var magnitude = totalVelocity.Magnitude();
        var azimuth = ComputeAzimuth(point, totalVelocity);

        return new VelocityDecomposition
        {
            RigidRotationComponent = rigidComponent,
            DeformationComponent = null, // No deformation model in this implementation
            RelativeToFrame = frame,
            Magnitude = magnitude,
            Azimuth = azimuth
        };
    }

    /// <summary>
    /// Gets the angular velocity of a reference frame.
    /// </summary>
    /// <param name="frame">The reference frame.</param>
    /// <param name="tick">The target simulation time.</param>
    /// <param name="kinematics">The kinematics state view.</param>
    /// <returns>Angular velocity of the frame.</returns>
    /// <remarks>
    /// <para>
    /// <b>⚠️ LIMITATION:</b> This is a simplified implementation with known limitations:
    /// </para>
    /// <para>
    /// <b>MantleFrame:</b> Returns zero (treats mantle as stationary reference).
    /// ⚠️ Full RFC-V2-0046 implementation should return the negative of the area-weighted
    /// net lithospheric rotation rate. Current implementation produces incorrect results
    /// if the net rotation is non-zero.
    /// </para>
    /// <para>
    /// <b>PlateAnchor:</b> Returns the angular velocity of the anchor plate. ✅ Fully implemented.
    /// </para>
    /// <para>
    /// <b>AbsoluteFrame:</b> Returns zero (no TPW integration).
    /// ⚠️ Full implementation should include True Polar Wander rotation rate when
    /// ITruePolarWanderModel is available. Current implementation ignores TPW motion.
    /// </para>
    /// <para>
    /// <b>CustomFrame:</b> Recursively evaluates base frame (assumes static transforms).
    /// ⚠️ Full implementation should compute time derivatives of custom frame transforms.
    /// Current implementation ignores time-dependent frame motion.
    /// </para>
    /// <para>
    /// TODO: Wire up mantle net-rotation provider, TPW model integration, and custom
    /// frame transform rates to achieve full RFC-V2-0046 compliance for frame-aware velocities.
    /// </para>
    /// </remarks>
    public AngularVelocity3d GetFrameAngularVelocity(
        ReferenceFrameId frame,
        CanonicalTick tick,
        IPlateKinematicsStateView kinematics)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(kinematics);

        // NOTE: This implementation has known limitations for MantleFrame, AbsoluteFrame, and CustomFrame.
        // See method documentation for details.
        return frame switch
        {
            MantleFrame => AngularVelocity3d.Zero, // TODO: Should query mantle net-rotation provider
            PlateAnchor anchor => _velocitySolver.GetAngularVelocity(kinematics, anchor.PlateId, tick),
            AbsoluteFrame => AngularVelocity3d.Zero, // TODO: Should integrate TPW rate when available
            CustomFrame custom => GetCustomFrameAngularVelocity(custom, tick, kinematics),
            _ => throw new NotSupportedException($"Frame type {frame.GetType().Name} is not supported.")
        };
    }

    /// <summary>
    /// Computes the rigid rotation velocity at a point.
    /// </summary>
    /// <param name="point">The position on the sphere.</param>
    /// <param name="angularVelocity">The angular velocity.</param>
    /// <returns>Linear velocity at the point due to rigid rotation (v = omega cross p).</returns>
    private static Velocity3d ComputeRigidVelocity(Vector3d point, AngularVelocity3d angularVelocity)
    {
        return angularVelocity.GetLinearVelocityAt(point.X, point.Y, point.Z);
    }

    /// <summary>
    /// Computes the azimuth of a velocity vector at a given point on the sphere.
    /// </summary>
    /// <param name="point">The position on the sphere (unit vector).</param>
    /// <param name="velocity">The velocity vector.</param>
    /// <returns>Azimuth angle in radians, clockwise from north (0 = north, pi/2 = east).</returns>
    private static double ComputeAzimuth(Vector3d point, Velocity3d velocity)
    {
        // If velocity is zero, return zero azimuth
        if (velocity.MagnitudeSquared() < double.Epsilon)
            return 0.0;

        // Normalize point to ensure it's on unit sphere
        var pointLength = Math.Sqrt(point.X * point.X + point.Y * point.Y + point.Z * point.Z);
        if (pointLength < double.Epsilon)
            return 0.0;

        var pn = new Vector3d(point.X / pointLength, point.Y / pointLength, point.Z / pointLength);

        // Compute local north direction at the point
        // North is the direction of increasing latitude, which is perpendicular to both
        // the radial direction and the east direction
        // For a point on the sphere, north = normalize(z_axis - (z_axis.dot(p)) * p) projected onto tangent plane

        // East direction: e = normalize(z_axis cross point)
        var zAxis = new Vector3d(0, 0, 1);
        var east = Cross(zAxis, pn);
        var eastLength = Math.Sqrt(east.X * east.X + east.Y * east.Y + east.Z * east.Z);

        // Handle poles where east is undefined
        if (eastLength < double.Epsilon)
        {
            // At poles, use x-axis as east
            east = new Vector3d(1, 0, 0);
            eastLength = 1.0;
        }
        else
        {
            east = new Vector3d(east.X / eastLength, east.Y / eastLength, east.Z / eastLength);
        }

        // North direction: n = point cross east (perpendicular to both radial and east)
        var north = Cross(pn, east);

        // Project velocity onto tangent plane (remove radial component)
        var velVec = new Vector3d(velocity.X, velocity.Y, velocity.Z);
        var radialComponent = Dot(velVec, pn);
        var tangentVel = new Vector3d(
            velVec.X - radialComponent * pn.X,
            velVec.Y - radialComponent * pn.Y,
            velVec.Z - radialComponent * pn.Z);

        // Decompose tangent velocity into north and east components
        var northComponent = Dot(tangentVel, north);
        var eastComponent = Dot(tangentVel, east);

        // Azimuth: angle from north, clockwise positive (0 = north, π/2 = east, π = south, 3π/2 = west)
        // atan2(east, north) gives angle from north in the range [-π, π]
        // Normalize to [0, 2π) for consistent output
        var azimuth = Math.Atan2(eastComponent, northComponent);
        if (azimuth < 0)
            azimuth += 2 * Math.PI;
        return azimuth;
    }

    /// <summary>
    /// Gets angular velocity for a custom frame by evaluating its chain.
    /// </summary>
    /// <remarks>
    /// ⚠️ LIMITATION: This implementation assumes custom frame transforms are static
    /// (no time-dependent rotation rates). It only accounts for the base frame's angular
    /// velocity. Full RFC-V2-0046 compliance would require computing the time derivative
    /// of the transform chain to capture frame motion.
    /// </remarks>
    private AngularVelocity3d GetCustomFrameAngularVelocity(
        CustomFrame custom,
        CanonicalTick tick,
        IPlateKinematicsStateView kinematics)
    {
        // Find the active link for this tick
        var activeLink = custom.Definition.Chain
            .Where(link => link.ValidityRange is null || link.ValidityRange.Value.Contains(tick))
            .OrderBy(link => link.SequenceHint ?? int.MaxValue)
            .FirstOrDefault();

        if (activeLink is null)
            return AngularVelocity3d.Zero;

        // Recursively get the base frame's angular velocity
        var baseFrameAngularVel = GetFrameAngularVelocity(activeLink.BaseFrame, tick, kinematics);

        // TODO: Add time derivative of activeLink.Transform to capture time-dependent frame motion
        // The custom frame's angular velocity is the base frame's angular velocity
        // plus any additional rotation rate from the transform (which is typically zero
        // for static custom frames)
        // For now, we assume custom frame transforms are static (no additional rotation rate)
        return baseFrameAngularVel;
    }

    private static Vector3d Cross(Vector3d a, Vector3d b)
    {
        return new Vector3d(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);
    }

    private static double Dot(Vector3d a, Vector3d b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }
}
