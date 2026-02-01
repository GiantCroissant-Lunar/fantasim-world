using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Velocity.Contracts;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Velocity.Solver;

using Vector3d = FantaSim.Geosphere.Plate.Topology.Contracts.Numerics.Vector3d;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct SampledPoint(Vector3d Position, int SegmentIndex, double SegmentT);

public sealed class RigidBoundaryVelocitySolver : IBoundaryVelocitySolver
{
    private const int DefaultSampleCount = 64;

    public static readonly BoundarySampleSpec DefaultSampling = new()
    {
        SampleCount = DefaultSampleCount,
        Mode = SamplingMode.ArcLength,
        Interpolation = InterpolationMethod.GreatCircle
    };

    private readonly IPlateVelocitySolver _velocitySolver;

    public RigidBoundaryVelocitySolver(IPlateVelocitySolver velocitySolver)
    {
        _velocitySolver = velocitySolver ?? throw new ArgumentNullException(nameof(velocitySolver));
    }

    public BoundaryVelocityProfile AnalyzeBoundary(
        Boundary boundary, BoundarySampleSpec sampling, CanonicalTick tick,
        IPlateTopologyStateView topology, IPlateKinematicsStateView kinematics)
    {
        ArgumentNullException.ThrowIfNull(boundary);
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

    /// <summary>
    /// Returns individual velocity samples for a boundary (internal, for testing).
    /// Used to verify RFC-V2-0034 invariants: tangent follows geometry, normal points left→right.
    /// </summary>
    internal BoundaryVelocitySample[] GetBoundarySamples(
        Boundary boundary, BoundarySampleSpec sampling, CanonicalTick tick,
        IPlateTopologyStateView topology, IPlateKinematicsStateView kinematics)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(kinematics);

        var vertices = ExtractVertices(boundary.Geometry);
        if (vertices.Length < 2)
            return Array.Empty<BoundaryVelocitySample>();

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

        return velocitySamples;
    }

    public BoundaryVelocityCollection AnalyzeAllBoundaries(
        IEnumerable<Boundary> boundaries, BoundarySampleSpec sampling, CanonicalTick tick,
        IPlateTopologyStateView topology, IPlateKinematicsStateView kinematics)
    {
        ArgumentNullException.ThrowIfNull(boundaries);
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

    private SampledPoint[] SampleBoundary(Vector3d[] vertices, BoundarySampleSpec sampling)
    {
        var includeEndpoints = sampling.JunctionBufferDistance is null or <= 0;

        if (sampling.Mode == SamplingMode.VertexOnly)
        {
            if (vertices.Length == 0)
                return Array.Empty<SampledPoint>();

            if (includeEndpoints || vertices.Length <= 2)
            {
                var all = new SampledPoint[vertices.Length];
                for (var i = 0; i < vertices.Length; i++)
                {
                    var segmentIndex = Math.Clamp(i, 0, Math.Max(0, vertices.Length - 2));
                    var segmentT = i == vertices.Length - 1 ? 1 : 0;
                    all[i] = new SampledPoint(vertices[i], segmentIndex, segmentT);
                }
                return all;
            }

            var interiorCount = Math.Max(0, vertices.Length - 2);
            var interior = new SampledPoint[interiorCount];
            for (var i = 0; i < interiorCount; i++)
                interior[i] = new SampledPoint(vertices[i + 1], i, 0);
            return interior;
        }

        if (vertices.Length == 2)
        {
            return includeEndpoints
                ? new[] { new SampledPoint(vertices[0], 0, 0), new SampledPoint(vertices[1], 0, 1) }
                : new[] { new SampledPoint(ComputeMidpoint(vertices[0], vertices[1]), 0, 0.5) };
        }

        var sampleCount = sampling.SampleCount ?? ComputeSampleCountFromSpacing(vertices, sampling, includeEndpoints) ?? DefaultSampleCount;
        if (sampleCount < 2) sampleCount = 2;

        return sampling.Mode switch
        {
            SamplingMode.ArcLength => SampleByArcLength(vertices, sampleCount, includeEndpoints),
            SamplingMode.ChordLength => SampleByChordLength(vertices, sampleCount, includeEndpoints),
            _ => SampleByArcLength(vertices, sampleCount, includeEndpoints)
        };
    }

    private static int? ComputeSampleCountFromSpacing(Vector3d[] vertices, BoundarySampleSpec sampling, bool includeEndpoints)
    {
        if (sampling.Spacing is null or <= 0)
            return null;

        double totalLength = 0.0;
        for (var i = 0; i < vertices.Length - 1; i++)
        {
            totalLength += sampling.Mode == SamplingMode.ChordLength
                ? (vertices[i + 1] - vertices[i]).Length()
                : GreatCircleDistance(vertices[i], vertices[i + 1]);
        }

        if (totalLength < double.Epsilon)
            return 2;

        var intervals = (int)Math.Max(1, Math.Round(totalLength / sampling.Spacing.Value));
        var count = intervals + 1;

        if (!includeEndpoints)
            count = Math.Max(1, count - 2);

        return count;
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

    /// <summary>
    /// Computes the boundary normal vector pointing from left plate toward right plate.
    ///
    /// The normal is computed as position × tangent (perpendicular to both the radial
    /// direction and the boundary tangent on the sphere surface). The sign is then
    /// chosen deterministically based on PlateId ordering:
    /// - If leftPlateId &lt; rightPlateId: normal points in cross product direction
    /// - If leftPlateId &gt; rightPlateId: normal points in negated cross product direction
    ///
    /// This ensures reproducible normal orientation across runs while maintaining
    /// the convention that positive normal rate indicates plates moving apart (divergent)
    /// and negative normal rate indicates plates moving together (convergent).
    /// </summary>
    private static Vector3d ComputeNormal(Vector3d position, Vector3d tangent, PlateId leftPlateId, PlateId rightPlateId)
    {
        // Compute the normal as position × tangent (perpendicular on sphere surface)
        var crossProduct = position.Cross(tangent);
        var crossLength = crossProduct.Length();

        if (crossLength < double.Epsilon)
        {
            // Degenerate case: use arbitrary perpendicular
            var arbitrary = Math.Abs(position.Dot(Vector3d.UnitX)) > 0.9 ? Vector3d.UnitY : Vector3d.UnitX;
            crossProduct = position.Cross(arbitrary).Normalize();
        }
        else
        {
            crossProduct = crossProduct / crossLength;
        }

        // Flip normal based on deterministic PlateId ordering rule:
        // Normal points from the plate with smaller ID toward the plate with larger ID.
        // This ensures consistent orientation regardless of which side is labeled "left".
        var shouldFlip = leftPlateId.Value > rightPlateId.Value;
        return shouldFlip ? -crossProduct : crossProduct;
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
