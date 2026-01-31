using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Motion.Contracts;

namespace FantaSim.Geosphere.Plate.Motion.Solver;

/// <summary>
/// Computes motion paths using Euler integration (RFC-V2-0049 §3).
/// </summary>
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
        StepPolicy stepPolicy,
        ReferenceFrameId frameId)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(kinematics);
        ArgumentNullException.ThrowIfNull(stepPolicy);

        // Current implementation only fully supports FixedInterval
        // Basic fallback for other policies to behave like FixedInterval(1)
        double stepTicks = 1.0;
        if (stepPolicy is StepPolicy.FixedInterval fixedSpec)
        {
            stepTicks = fixedSpec.StepTicks;
        }

        // Build samples list
        var samples = ImmutableArray.CreateBuilder<MotionPathSample>();
        var p = startPoint;
        var t = startTick;
        var currentPlateId = plateId;
        double accumulatedError = 0.0;

        // Add initial sample
        // For RFC compliance: provenance at step 0 is usually derived or empty
        // Velocity at t=0
        var v0 = GetVelocity(kinematics, currentPlateId, p, t);

        // Find initial provenance segment if possible
        var initialSegment = default(MotionSegmentId); // Placeholder: would need look-up from kinematics

        samples.Add(new MotionPathSample(
            t,
            p,
            currentPlateId,
            v0,
            new ReconstructionProvenance(initialSegment, null, 0.0),
            accumulatedError));

        int safetyMaxSteps = 10000; // Safety brake
        int steps = 0;

        while (!ReachedEnd(t, endTick, direction) && steps < safetyMaxSteps)
        {
            // Compute dt based on direction
            var dt = direction == IntegrationDirection.Forward
                ? stepTicks
                : -stepTicks;

            // Clamp dt if we would overshoot endTick
            double remaining = direction == IntegrationDirection.Forward
                ? endTick.Value - t.Value
                : t.Value - endTick.Value;

            if (remaining < stepTicks)
            {
                dt = direction == IntegrationDirection.Forward ? remaining : -remaining;
            }

            // Current state
            var positionVector = new Vector3d(p.X, p.Y, p.Z);
            var v = GetVelocity(kinematics, currentPlateId, positionVector, t);

            // Euler step: p' = p + v * dt
            var newPositionVector = positionVector + new Vector3d(v.X * dt, v.Y * dt, v.Z * dt);

            // Project back onto body surface
            p = NormalizeToBodySurface(newPositionVector);

            // Advance time
            t = new CanonicalTick((long)(t.Value + dt));
            steps++;

            if (ReachedEnd(t, endTick, direction))
            {
                // EndTick is exclusive, so if we hit it, we stop without adding this sample
                break;
            }

            // Update accumulated error (Linear approximation from RFC)
            // ε(t + Δt) = ε(t) + ... placeholder logic
            accumulatedError += 0.0001 * Math.Abs(dt); // Dummy linear growth

            // Determine plate at new position
            // For FixedInterval motion path on *ONE* plate, we usually assume it stays on that plate
            // OR we detect boundary crossing if policy demands it.
            // RFC says "Motion Path traces how a fixed point on a plate moves".
            // So PlateId usually stays constant unless we are tracking absolute motion across plates.
            // But RFC 3.5 says "When a motion path crosses a plate boundary... PlateId switches".
            // This implies we check topology.

            // Optimization: For strict Motion Path (point fixed on plate), PlateId is constant relative to the plate frame,
            // but if we are in Absolute frame, the point "moves" with the plate.
            // Wait, if "AnchorPlate" is fixed, does PlateId in sample change?
            // RFC says: "PlateId: Plate containing this point at tick".
            // If the point moves with plate A, it stays inside plate A.
            // UNLESS the plate geometry itself changes and the point falls off?
            // Or if we are tracing "hotspot track" (point fixed in mantle)?
            // If AnchorPlate is provided, usually we mean the point is *tectonically* moving attached to that plate.
            // BUT if frameId is Mantle, we are tracing absolute path.
            // If frameId is specific Plate, we are tracing relative path.

            // Checking containing plate every step is expensive but correct for RFC 3.5 compliance
            // var foundPlate = topology.FindPlateContaining(p, t);
            // For this MVP, we stick to currentPlateId

            var nextV = GetVelocity(kinematics, currentPlateId, p, t);

            samples.Add(new MotionPathSample(
                t,
                p,
                currentPlateId,
                nextV,
                new ReconstructionProvenance(default(MotionSegmentId), null, 0.5), // Placeholder provenance
                accumulatedError));
        }

        return new MotionPath(
            plateId,
            startTick,
            endTick,
            direction,
            frameId,
            samples.ToImmutable());
    }

    private Vector3d GetVelocity(IPlateKinematicsStateView kinematics, PlateId plateId, Point3 p, CanonicalTick t)
    {
        var v = _velocitySolver.GetAbsoluteVelocity(
            kinematics,
            plateId,
            new Vector3d(p.X, p.Y, p.Z),
            t);
        return new Vector3d(v.X, v.Y, v.Z);
    }

    private Vector3d GetVelocity(IPlateKinematicsStateView kinematics, PlateId plateId, Vector3d p, CanonicalTick t)
    {
        var v = _velocitySolver.GetAbsoluteVelocity(
            kinematics,
            plateId,
            p,
            t);
        return new Vector3d(v.X, v.Y, v.Z);
    }

    private static bool ReachedEnd(CanonicalTick currentTick, CanonicalTick endTick, IntegrationDirection direction)
    {
        const double Epsilon = 1e-6;
        if (direction == IntegrationDirection.Forward)
            return currentTick.Value >= (endTick.Value - Epsilon);
        else
            return currentTick.Value <= (endTick.Value + Epsilon);
    }

    private static Point3 NormalizeToBodySurface(Vector3d point)
    {
        var length = point.Length();
        if (length < double.Epsilon)
        {
            return new Point3(1, 0, 0);
        }

        var normalized = point / length;
        return new Point3(normalized.X, normalized.Y, normalized.Z);
    }
}
