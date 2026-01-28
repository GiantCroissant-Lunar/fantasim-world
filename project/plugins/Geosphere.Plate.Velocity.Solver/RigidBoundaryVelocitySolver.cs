using System.Collections.Immutable;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Velocity.Solver;

using Vector3d = FantaSim.Geosphere.Plate.Topology.Contracts.Numerics.Vector3d;

internal readonly record struct SampledPoint(Vector3d Position, int SegmentIndex, double SegmentT);

public sealed class RigidBoundaryVelocitySolver : IBoundaryVelocitySolver
{
    public static readonly BoundarySamplingSpec DefaultSampling = new(
        SampleCount: 64, Mode: SamplingMode.ArcLength, IncludeEndpoints: true);

    private readonly IPlateVelocitySolver _velocitySolver;

    public RigidBoundaryVelocitySolver(IPlateVelocitySolver velocitySolver)
    {
        _velocitySolver = velocitySolver ?? throw new ArgumentNullException(nameof(velocitySolver));
    }

    public BoundaryVelocityProfile AnalyzeBoundary(
        Boundary boundary, BoundarySamplingSpec sampling, CanonicalTick tick,
        IPlateTopologyStateView topology, IPlateKinematicsStateView kinematics)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        ArgumentNullException.ThrowIfNull(sampling);
        ArgumentNullException.ThrowIfNull(tick);
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(kinematics);

        var vertices = ExtractVertices(boundary.Geometry);
        if (vertices.Length < 2)
            return CreateEmptyProfile(boundary.BoundaryId);

        var sampledPoints = SampleBoundary(vertices, sampling);
        var leftOmega = _velocitySolver.GetAngularVelocity(kinematics, boundary.PlateIdLeft, tick);
        var rightOmega = _velocitySolver.GetAngularVelocity(kinematics, boundary.PlateIdRight, tick);

        var velocitySamples = new BoundaryVelocitySample[sampledPoints.Length];
        for (var i = 0; i < sampledPoints.Length; i++)
        {
            velocitySamples[i] = ComputeSampleVelocity(
                sampledPoints[i], vertices, boundary.PlateIdLeft, boundary.PlateIdRight,
                leftOmega, rightOmega, tick, kinematics, i);
        }

        return ComputeAggregates(boundary.BoundaryId, velocitySamples);
    }

    public BoundaryVelocityCollection AnalyzeAllBoundaries(
        IEnumerable<Boundary> boundaries, BoundarySamplingSpec sampling, CanonicalTick tick,
        IPlateTopologyStateView topology, IPlateKinematicsStateView kinematics)
    {
        ArgumentNullException.ThrowIfNull(boundaries);
        ArgumentNullException.ThrowIfNull(sampling);
        ArgumentNullException.ThrowIfNull(tick);
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(kinematics);

        var profiles = boundaries.Where(b => !b.IsRetired)
            .Select(b => AnalyzeBoundary(b, sampling, tick, topology, kinematics))
            .OrderBy(p => p.BoundaryId.Value)
            .ToImmutableArray();

        return new BoundaryVelocityCollection(tick, profiles, nameof(RigidBoundaryVelocitySolver));
    }

    private static Vector3d[] ExtractVertices(IGeometry geometry)
    {
        if (geometry is not Polyline3 polyline || polyline.IsEmpty) return Array.Empty<Vector3d>();
        var vertices = new Vector3d[polyline.PointCount];
        for (var i = 0; i < polyline.PointCount; i++)
        {
            var point = polyline[i];
            vertices[i] = new Vector3d(point.X, point.Y, point.Z);
        }
        return vertices;
    }

    private SampledPoint[] SampleBoundary(Vector3d[] vertices, BoundarySamplingSpec sampling)
    {
        var sampleCount = sampling.SampleCount < 2 ? 2 : sampling.SampleCount;
        if (vertices.Length == 2)
        {
            return sampling.IncludeEndpoints
                ? new[] { new SampledPoint(vertices[0], 0, 0), new SampledPoint(vertices[1], 0, 1) }
                : new[] { new SampledPoint(ComputeMidpoint(vertices[0], vertices[1]), 0, 0.5) };
        }
        return sampling.Mode switch
        {
            SamplingMode.ArcLength => SampleByArcLength(vertices, sampleCount, sampling.IncludeEndpoints),
            SamplingMode.ChordLength => SampleByChordLength(vertices, sampleCount, sampling.IncludeEndpoints),
            _ => SampleByArcLength(vertices, sampleCount, sampling.IncludeEndpoints)
        };
    }

    private static SampledPoint[] SampleByArcLength(Vector3d[] vertices, int sampleCount, bool includeEndpoints)
    {
        var segmentLengths = new double[vertices.Length - 1];
        var totalLength = 0.0;
        for (var i = 0; i < vertices.Length - 1; i++)
        {
            segmentLengths[i] = GreatCircleDistance(vertices[i], vertices[i + 1]);
            totalLength += segmentLengths[i];
        }
        if (totalLength < double.Epsilon) return new[] { new SampledPoint(vertices[0], 0, 0) };

        var effectiveCount = includeEndpoints ? sampleCount : sampleCount - 2;
        var effectiveStart = includeEndpoints ? 0 : 1;
        var effectiveEnd = includeEndpoints ? sampleCount - 1 : sampleCount - 2;
        var samples = new SampledPoint[sampleCount];

        if (includeEndpoints)
        {
            samples[0] = new SampledPoint(vertices[0], 0, 0);
            samples[sampleCount - 1] = new SampledPoint(vertices[^1], vertices.Length - 2, 1);
        }

        var currentSegment = 0;
        var remainingInSegment = segmentLengths.Length > 0 ? segmentLengths[0] : 0;

        for (var i = effectiveStart; i <= effectiveEnd; i++)
        {
            var targetDistance = (i * totalLength) / (effectiveCount + 1);
            while (currentSegment < segmentLengths.Length - 1 && targetDistance > totalLength - remainingInSegment)
            {
                targetDistance -= remainingInSegment;
                currentSegment++;
                remainingInSegment = segmentLengths[currentSegment];
            }
            var segmentProgress = remainingInSegment > double.Epsilon ? targetDistance / remainingInSegment : 0.0;
            var start = vertices[currentSegment];
            var end = vertices[currentSegment + 1];
            var interpolated = segmentProgress <= 0 ? start : (segmentProgress >= 1 ? end : GreatCircleInterpolate(start, end, segmentProgress));
            samples[i] = new SampledPoint(interpolated, currentSegment, Math.Clamp(segmentProgress, 0, 1));
        }
        return samples;
    }

    private static SampledPoint[] SampleByChordLength(Vector3d[] vertices, int sampleCount, bool includeEndpoints)
    {
        var segmentLengths = new double[vertices.Length - 1];
        var totalLength = 0.0;
        for (var i = 0; i < vertices.Length - 1; i++)
        {
            segmentLengths[i] = (vertices[i + 1] - vertices[i]).Length();
            totalLength += segmentLengths[i];
        }
        if (totalLength < double.Epsilon) return new[] { new SampledPoint(vertices[0], 0, 0) };

        var effectiveCount = includeEndpoints ? sampleCount : sampleCount - 2;
        var effectiveStart = includeEndpoints ? 0 : 1;
        var effectiveEnd = includeEndpoints ? sampleCount - 1 : sampleCount - 2;
        var samples = new SampledPoint[sampleCount];

        if (includeEndpoints)
        {
            samples[0] = new SampledPoint(vertices[0], 0, 0);
            samples[sampleCount - 1] = new SampledPoint(vertices[^1], vertices.Length - 2, 1);
        }

        var currentSegment = 0;
        var remainingInSegment = segmentLengths.Length > 0 ? segmentLengths[0] : 0;

        for (var i = effectiveStart; i <= effectiveEnd; i++)
        {
            var targetDistance = (i * totalLength) / (effectiveCount + 1);
            while (currentSegment < segmentLengths.Length - 1 && targetDistance > totalLength - remainingInSegment)
            {
                targetDistance -= remainingInSegment;
                currentSegment++;
                remainingInSegment = segmentLengths[currentSegment];
            }
            var segmentProgress = remainingInSegment > double.Epsilon ? targetDistance / remainingInSegment : 0.0;
            var interpolated = vertices[currentSegment] + (vertices[currentSegment + 1] - vertices[currentSegment]) * segmentProgress;
            samples[i] = new SampledPoint(interpolated, currentSegment, Math.Clamp(segmentProgress, 0, 1));
        }
        return samples;
    }

    private static double GreatCircleDistance(Vector3d a, Vector3d b)
    {
        var dot = a.Dot(b);
        var cross = a.Cross(b).Length();
        return Math.Atan2(cross, dot);
    }

    private static Vector3d GreatCircleInterpolate(Vector3d a, Vector3d b, double t)
    {
        var aNorm = a.LengthSquared() > 0.9 && a.LengthSquared() < 1.1 ? a : a.Normalize();
        var bNorm = b.LengthSquared() > 0.9 && b.LengthSquared() < 1.1 ? b : b.Normalize();
        var dot = Math.Clamp(aNorm.Dot(bNorm), -1.0, 1.0);
        var angle = Math.Acos(dot);
        if (angle < double.Epsilon) return aNorm;
        var sinAngle = Math.Sin(angle);
        return aNorm * (Math.Sin((1 - t) * angle) / sinAngle) + bNorm * (Math.Sin(t * angle) / sinAngle);
    }

    private BoundaryVelocitySample ComputeSampleVelocity(
        SampledPoint sampledPoint, Vector3d[] vertices, PlateId leftPlateId, PlateId rightPlateId,
        AngularVelocity3d leftOmega, AngularVelocity3d rightOmega, CanonicalTick tick,
        IPlateKinematicsStateView kinematics, int sampleIndex)
    {
        var position = sampledPoint.Position;
        var vLeft = leftOmega.GetLinearVelocityAt(position.X, position.Y, position.Z);
        var vRight = rightOmega.GetLinearVelocityAt(position.X, position.Y, position.Z);
        var vRel = vRight - vLeft;
        var tangent = ComputeTangentFromSegment(sampledPoint, vertices);
        var normal = ComputeNormal(position, tangent, leftPlateId, rightPlateId);
        return new BoundaryVelocitySample(position, vRel, tangent, normal, vRel.Dot(tangent), vRel.Dot(normal), sampleIndex);
    }

    private static Vector3d ComputeTangentFromSegment(SampledPoint sampledPoint, Vector3d[] vertices)
    {
        var segmentIndex = sampledPoint.SegmentIndex;
        if (segmentIndex < 0 || segmentIndex >= vertices.Length - 1)
        {
            if (segmentIndex == vertices.Length - 1 && vertices.Length > 1) segmentIndex = vertices.Length - 2;
            else return Vector3d.UnitZ;
        }
        var start = vertices[segmentIndex];
        var end = vertices[segmentIndex + 1];
        var direction = end - start;
        var length = direction.Length();
        if (length < double.Epsilon)
        {
            if (segmentIndex > 0) { start = vertices[segmentIndex - 1]; end = vertices[segmentIndex]; direction = end - start; length = direction.Length(); }
            if (length < double.Epsilon) return Vector3d.UnitZ;
        }
        return direction / length;
    }

    private static Vector3d ComputeNormal(Vector3d position, Vector3d tangent, PlateId leftPlateId, PlateId rightPlateId)
    {
        var crossProduct = position.Cross(tangent);
        var crossLength = crossProduct.Length();
        if (crossLength < double.Epsilon)
        {
            var arbitrary = Math.Abs(position.Dot(Vector3d.UnitX)) > 0.9 ? Vector3d.UnitY : Vector3d.UnitX;
            crossProduct = position.Cross(arbitrary).Normalize();
        }
        else crossProduct = crossProduct / crossLength;
        var leftToRight = leftPlateId.Value > rightPlateId.Value ? -Vector3d.UnitX : Vector3d.UnitX;
        return crossProduct.Dot(leftToRight) < 0 ? -crossProduct : crossProduct;
    }

    private static BoundaryVelocityProfile ComputeAggregates(BoundaryId boundaryId, BoundaryVelocitySample[] samples)
    {
        if (samples.Length == 0)
            return new BoundaryVelocityProfile(boundaryId, 0, 0, 0, 0, 0, 0, 0);
        var minNormal = double.MaxValue; var maxNormal = double.MinValue;
        var sumNormal = 0.0; var sumSlip = 0.0; var minIndex = 0; var maxIndex = 0;
        for (var i = 0; i < samples.Length; i++)
        {
            var normalRate = samples[i].NormalRate;
            var slipRate = Math.Abs(samples[i].TangentialRate);
            if (normalRate < minNormal) { minNormal = normalRate; minIndex = i; }
            if (normalRate > maxNormal) { maxNormal = normalRate; maxIndex = i; }
            sumNormal += normalRate; sumSlip += slipRate;
        }
        return new BoundaryVelocityProfile(boundaryId, samples.Length, minNormal, maxNormal, sumNormal / samples.Length, sumSlip / samples.Length, minIndex, maxIndex);
    }

    private static BoundaryVelocityProfile CreateEmptyProfile(BoundaryId boundaryId)
        => new(boundaryId, 0, 0, 0, 0, 0, 0, 0);

    private static Vector3d ComputeMidpoint(Vector3d a, Vector3d b)
        => new((a.X + b.X) / 2, (a.Y + b.Y) / 2, (a.Z + b.Z) / 2);
}
