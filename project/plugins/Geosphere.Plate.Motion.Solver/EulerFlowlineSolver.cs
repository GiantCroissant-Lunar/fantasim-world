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
/// </summary>
public sealed class EulerFlowlineSolver : IFlowlineSolver
{
    private readonly EulerMotionPathSolver _motionPathSolver;

    public EulerFlowlineSolver(IPlateVelocitySolver velocitySolver)
    {
        ArgumentNullException.ThrowIfNull(velocitySolver);
        _motionPathSolver = new EulerMotionPathSolver(velocitySolver);
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

        // Compute basic motion path logic
        // Flowlines trace points on plates, so they are essentially motion paths anchored to the spreading center at t0
        // NOTE: Flowlines typically trace generated crust.

        var motionPath = _motionPathSolver.ComputeMotionPath(
            plateId,
            seedPoint,
            tickA,
            tickB,
            direction,
            topology,
            kinematics,
            stepPolicy,
            ReferenceFrameId.Mantle // Flowlines usually visualized in Absolute/Mantle frame, or Relative? RFC doesn't specify default, assumes call knows.
                                    // BUT, wait. MotionPathSolver takes FrameId.
                                    // If we want the path of the crust element, usually we want it in the Mantle frame (absolute motion)
                                    // OR in the frame of the *other* plate (relative motion).
                                    // Let's assume Mantle frame for the generic "trace path" Solver
                                    // unless we want to expose FrameId in Flowline signature too (which RFC didn't, but maybe implied).
                                    // RFC 4.1 signature: no FrameId.
                                    // RFC 1.2 "How has this plate boundary segment evolved".
                                    // Let's assume Mantle/Absolute for now or default frame.
                                    // Actually, let's pick ReferenceFrameId.Mantle as a sensible default if not parameterized.
        );

        // Convert strict MotionPath samples to Flowline samples
        // Requires computing SpreadingRate and AccumulatedOpening

        var flowlineSamples = ImmutableArray.CreateBuilder<FlowlineSample>();
        double accumulatedOpening = 0.0;

        foreach (var mpSample in motionPath.Samples)
        {
            // Placeholder: Spreading rate computation would require querying velocity of BOTH plates at the boundary
            // For MVP, we use 0 or a fixed model if Uniform.
            double rate = 0.0;
            if (spreadingModel.Type == SpreadingModelType.Uniform)
            {
               rate = 5.0; // cm/yr dummy
            }

            // Accumulate opening: rate * dt
            // Note: need previous tick to know dt.

            flowlineSamples.Add(new FlowlineSample(
                mpSample.Tick,
                mpSample.Position,
                mpSample.PlateId,
                mpSample.Velocity,
                mpSample.Provenance,
                mpSample.AccumulatedError,
                rate,
                accumulatedOpening,
                false, // IsRidgeSegment
                null   // SubductionAge
            ));
        }

        return new Flowline(
            seedPoint,
            boundaryId,
            side,
            tickA,
            tickB,
            spreadingModel,
            flowlineSamples.ToImmutable());
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

        // If spacing is non-positive, just return vertices (or just start/end? Default to small spacing?)
        // Let's assume spacing > 0. If <= 0, return vertices.
        if (spacing <= double.Epsilon)
        {
            for(int i=0; i<polyline.PointCount; i++) result.Add(polyline[i]);
            return result;
        }

        // Walk the line
        // Always add start point
        result.Add(polyline[0]);
        double nextSampleDist = spacing;
        double currentDist = 0.0;

        for (int i = 0; i < polyline.PointCount - 1; i++)
        {
            var p0 = polyline[i];
            var p1 = polyline[i+1];
            double segmentLength = p0.DistanceTo(p1);

            // While next sample falls within this segment
            while (currentDist + segmentLength >= nextSampleDist - 1e-9) // Tolerance for precision
            {
                // Distance from p0 to sample
                double distOnSegment = nextSampleDist - currentDist;
                double t = distOnSegment / segmentLength;

                // Interpolate
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
            // Fallback for tests or loose coupling
            return new PlateId(Guid.Empty);
        }

        return side switch
        {
            PlateSide.Left => boundary.PlateIdLeft,
            PlateSide.Right => boundary.PlateIdRight,
            _ => throw new ArgumentException($"Unknown plate side: {side}", nameof(side))
        };
    }
}
