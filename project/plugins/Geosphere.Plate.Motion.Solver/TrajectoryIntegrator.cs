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
/// Core integrator for trajectories (RFC-V2-0049a).
/// </summary>
public sealed class TrajectoryIntegrator : ITrajectoryIntegrator
{
    private readonly IPlateVelocitySolver _velocitySolver;

    public TrajectoryIntegrator(IPlateVelocitySolver velocitySolver)
    {
        _velocitySolver = velocitySolver ?? throw new ArgumentNullException(nameof(velocitySolver));
    }

    public Trajectory Integrate(TrajectoryIntegrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Resolve generic step policy
        double stepTicks = 1.0;
        if (context.Policy is StepPolicy.FixedInterval fixedSpec)
        {
            stepTicks = fixedSpec.StepTicks;
        }

        var samples = ImmutableArray.CreateBuilder<TrajectorySample>();
        var p = context.SeedPoint;
        var t = context.StartTick;
        var currentPlateId = context.SeedPlateId;
        double totalError = 0.0;
        int crossingCount = 0;
        int steps = 0;
        const int SafetyMaxSteps = 10000;

        // Initial sample
        var v0 = GetVelocity(context.Kinematics, currentPlateId, p, t);

        // Initial sample provenance
        var initialProv = new SampleProvenance
        {
            ReconstructionInfo = new ReconstructionProvenance(default(MotionSegmentId), null, 0.0)
        };

        var initialSample = new TrajectorySample
        {
            Tick = t,
            Position = p,
            PlateId = currentPlateId,
            Velocity = v0,
            AccumulatedError = 0.0,
            Provenance = initialProv,
            CrossedBoundary = null
        };

        samples.Add(initialSample);

        // Check custom sampler for initial point
        TrajectoryIntegrationState state = CreateState(initialSample, steps);
        if (context.CustomSampler != null)
        {
             // If custom sampler wants to add metadata or control something, we can let it.
             // RFC implies ShouldSample determines if we *keep* a sample.
             // But usually start point is always kept.
        }

        while (!ReachedEnd(t, context.EndTick, context.Direction) && steps < SafetyMaxSteps)
        {
            // Compute dt
            var dt = context.Direction == IntegrationDirection.Forward
                ? stepTicks
                : -stepTicks;

            // Clamp dt
            double remaining = context.Direction == IntegrationDirection.Forward
                ? context.EndTick.Value - t.Value
                : t.Value - context.EndTick.Value;

            if (remaining < stepTicks)
            {
                dt = context.Direction == IntegrationDirection.Forward ? remaining : -remaining;
            }

            var positionVector = new Vector3d(p.X, p.Y, p.Z);
            var v = GetVelocity(context.Kinematics, currentPlateId, positionVector, t);

            // Euler step
            var newPositionVector = positionVector + (v * dt);

            // Normalize
            p = NormalizeToBodySurface(newPositionVector);

            // Advance time
            t = new CanonicalTick((long)(t.Value + dt));
            steps++;

            if (ReachedEnd(t, context.EndTick, context.Direction))
            {
                break;
            }

            // Error accumulation
            totalError += 0.0001 * Math.Abs(dt);

            // Velocity at new state
            var nextV = GetVelocity(context.Kinematics, currentPlateId, p, t);

            var sampleProv = new SampleProvenance
            {
                ReconstructionInfo = new ReconstructionProvenance(default(MotionSegmentId), null, 0.5)
            };

            var sample = new TrajectorySample
            {
                Tick = t,
                Position = p,
                PlateId = currentPlateId,
                Velocity = nextV,
                AccumulatedError = totalError,
                Provenance = sampleProv,
                CrossedBoundary = null
            };

            // Custom sampler check
            bool keepSample = true;
            if (context.CustomSampler != null)
            {
                state = CreateState(sample, steps, samples[samples.Count - 1]);
                keepSample = context.CustomSampler.ShouldSample(state);
            }

            if (keepSample)
            {
                samples.Add(sample);
            }
        }

        return new Trajectory
        {
            Samples = samples.ToImmutable(),
            TotalAccumulatedError = totalError,
            BoundaryCrossingCount = crossingCount,
            Provenance = new TrajectoryProvenance { IntegratorVersion = "Euler-1.0" },
            Statistics = new IntegrationStatistics { SampleCount = samples.Count, ComputationTimeMs = 0 }
        };
    }

    private TrajectoryIntegrationState CreateState(TrajectorySample sample, int stepCount, TrajectorySample? prev = null)
    {
        return new TrajectoryIntegrationState
        {
            CurrentTick = sample.Tick,
            CurrentPosition = sample.Position,
            CurrentPlateId = sample.PlateId,
            CurrentVelocity = sample.Velocity,
            AccumulatedError = sample.AccumulatedError,
            StepCount = stepCount,
            PreviousSample = prev
        };
    }

    private Vector3d GetVelocity(IPlateKinematicsStateView kinematics, PlateId plateId, Point3 p, CanonicalTick t)
    {
        return GetVelocity(kinematics, plateId, new Vector3d(p.X, p.Y, p.Z), t);
    }

    private Vector3d GetVelocity(IPlateKinematicsStateView kinematics, PlateId plateId, Vector3d p, CanonicalTick t)
    {
        var v = _velocitySolver.GetAbsoluteVelocity(kinematics, plateId, p, t);
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
