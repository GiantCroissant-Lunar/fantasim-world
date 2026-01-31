using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using UnifyGeometry;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using FantaSim.Geosphere.Plate.Motion.Contracts;

namespace FantaSim.Geosphere.Plate.Motion.Solver;

/// <summary>
/// Computes flowlines using Euler integration (RFC-V2-0049 §4).
/// Refactored to use shared TrajectoryIntegrator (RFC-V2-0049a).
/// </summary>
public sealed class EulerFlowlineSolver : IFlowlineSolver
{
    private readonly ITrajectoryIntegrator _integrator;

    public EulerFlowlineSolver(IPlateVelocitySolver velocitySolver)
    {
        ArgumentNullException.ThrowIfNull(velocitySolver);
        _integrator = new TrajectoryIntegrator(velocitySolver);
    }

    public EulerFlowlineSolver(ITrajectoryIntegrator integrator)
    {
        _integrator = integrator ?? throw new ArgumentNullException(nameof(integrator));
    }

    public Flowline ComputeFlowline(
        UnifyGeometry.Point3 seedPoint,
        BoundaryId boundaryId,
        PlateSide side,
        SpreadingModel spreadingModel,
        CanonicalTick tickA,
        CanonicalTick tickB,
        StepPolicy stepPolicy,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(kinematics);

        // Resolve the plate based on side
        var plateId = ResolvePlateId(boundaryId, side, topology);

        // Compute integration direction from ticks
        var direction = tickB.Value >= tickA.Value
            ? IntegrationDirection.Forward
            : IntegrationDirection.Backward;

        // Create specialized sampler
        var sampler = new FlowlineSampler(spreadingModel);

        var context = new TrajectoryIntegrationContext
        {
            SeedPoint = seedPoint,
            SeedPlateId = plateId,
            StartTick = tickA,
            EndTick = tickB,
            Policy = stepPolicy,
            Topology = topology,
            Kinematics = kinematics,
            Direction = direction,
            CustomSampler = sampler
        };

        var trajectory = _integrator.Integrate(context);

        // Convert generic Trajectory to Flowline
        var flowlineSamples = trajectory.Samples.Select(s => new FlowlineSample(
            s.Tick,
            s.Position,
            s.PlateId,
            s.Velocity,
            s.Provenance.ReconstructionInfo,
            s.AccumulatedError,
            sampler.GetSpreadingRate(s.Tick),
            sampler.GetAccumulatedOpening(s.Tick),
            false, // IsRidgeSegment placeholder
            null   // SubductionAge placeholder
        ));

        return new Flowline(
            seedPoint,
            boundaryId,
            side,
            tickA,
            tickB,
            spreadingModel,
            flowlineSamples.ToImmutableArray());
    }

    public ImmutableArray<Flowline> ComputeFlowlineBundle(
        BoundaryId boundaryId,
        PlateSide side,
        double sampleSpacing,
        SpreadingModel spreadingModel,
        CanonicalTick tickA,
        CanonicalTick tickB,
        StepPolicy stepPolicy,
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics)
    {
        if (!topology.Boundaries.TryGetValue(boundaryId, out var boundary))
        {
            return ImmutableArray<Flowline>.Empty;
        }

        if (boundary.Geometry is not Polyline3 polyline || polyline.PointCount < 2)
        {
             return ImmutableArray<Flowline>.Empty;
        }

        var seeds = SamplePoints(polyline, sampleSpacing);
        var builder = ImmutableArray.CreateBuilder<Flowline>(seeds.Count);

        foreach (var seed in seeds)
        {
            builder.Add(ComputeFlowline(
                seed,
                boundaryId,
                side,
                spreadingModel,
                tickA,
                tickB,
                stepPolicy,
                topology,
                kinematics));
        }

        return builder.ToImmutable();
    }

    private static List<UnifyGeometry.Point3> SamplePoints(Polyline3 polyline, double spacing)
    {
        var result = new List<UnifyGeometry.Point3>();
        if (polyline.PointCount == 0) return result;

        if (spacing <= double.Epsilon)
        {
            for(int i=0; i<polyline.PointCount; i++) result.Add(polyline[i]);
            return result;
        }

        result.Add(polyline[0]);
        double nextSampleDist = spacing;
        double currentDist = 0.0;

        for (int i = 0; i < polyline.PointCount - 1; i++)
        {
            var p0 = polyline[i];
            var p1 = polyline[i+1];
            double segmentLength = p0.DistanceTo(p1);

            while (currentDist + segmentLength >= nextSampleDist - 1e-9)
            {
                double distOnSegment = nextSampleDist - currentDist;
                double t = distOnSegment / segmentLength;

                var x = p0.X + (p1.X - p0.X) * t;
                var y = p0.Y + (p1.Y - p0.Y) * t;
                var z = p0.Z + (p1.Z - p0.Z) * t;

                result.Add(new UnifyGeometry.Point3(x, y, z));
                nextSampleDist += spacing;
            }

            currentDist += segmentLength;
        }

        return result;
    }

    private static PlateId ResolvePlateId(
        BoundaryId boundaryId,
        PlateSide side,
        IPlateTopologyStateView topology)
    {
        if (!topology.Boundaries.TryGetValue(boundaryId, out var boundary))
        {
            return new PlateId(Guid.Empty);
        }

        return side switch
        {
            PlateSide.Left => boundary.PlateIdLeft,
            PlateSide.Right => boundary.PlateIdRight,
            _ => throw new ArgumentException($"Unknown plate side: {side}", nameof(side))
        };
    }

    /// <summary>
    /// Captures spreading data during integration.
    /// </summary>
    private sealed class FlowlineSampler : ITrajectorySampler
    {
        private readonly SpreadingModel _spreadingModel;
        private readonly Dictionary<long, double> _rates = new();
        private double _accumulatedOpening = 0.0;
        private readonly Dictionary<long, double> _openings = new();

        public FlowlineSampler(SpreadingModel model)
        {
            _spreadingModel = model;
        }

        public bool ShouldSample(TrajectoryIntegrationState state)
        {
            // Compute rate for this step
            // MVP: Use constant or model
            double rate = 0.0;
            if (_spreadingModel.Type == SpreadingModelType.Uniform)
            {
                 rate = 5.0;
            }

            _rates[state.CurrentTick.Value] = rate;

            // Accumulate opening
            // dt is roughly needed, but state doesn't have dt directly, only Tick.
            // We can infer dt from previous sample.
            double dt = 0.0;
            if (state.PreviousSample != null)
            {
                dt = Math.Abs(state.CurrentTick.Value - state.PreviousSample.Tick.Value);
            }
            else if (state.StepCount == 0)
            {
                 // Initial step
            }

            _accumulatedOpening += rate * dt;
            _openings[state.CurrentTick.Value] = _accumulatedOpening;

            return true;
        }

        public Dictionary<string, object> ComputeMetadata(TrajectoryIntegrationState state)
        {
            return new Dictionary<string, object>();
        }

        public double GetSpreadingRate(CanonicalTick tick)
        {
            return _rates.TryGetValue(tick.Value, out var val) ? val : 0.0;
        }

        public double GetAccumulatedOpening(CanonicalTick tick)
        {
            return _openings.TryGetValue(tick.Value, out var val) ? val : 0.0;
        }
    }
}
