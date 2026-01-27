using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Reconstruction.Solver;

/// <summary>
/// Minimal reconstruction solver implementation for RFC-V2-0024.
///
/// Current v0 behavior:
/// - Uses topology slice boundaries at the provided tick (topology already time-cutoff materialized by caller).
/// - Attaches single-plate provenance (left plate) per boundary.
/// - Returns geometry without applying kinematic transforms (geometry rotation is future work once spherical geometry types land).
/// </summary>
public sealed class NaivePlateReconstructionSolver : IPlateReconstructionSolver
{
    public IReadOnlyList<ReconstructedBoundary> ReconstructBoundaries(
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        CanonicalTick targetTick,
        ReconstructionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(kinematics);

        var boundaries = topology.Boundaries.Values
            .Where(b => !b.IsRetired)
            .OrderBy(b => b.BoundaryId.Value)
            .Select(b =>
            {
                var rotation = Quaterniond.Identity;
                if (kinematics.TryGetRotation(b.PlateIdLeft, targetTick, out var r))
                    rotation = r;

                var geometry = ApplyRotation(b.Geometry, rotation);

                return new ReconstructedBoundary(
                    b.BoundaryId,
                    b.PlateIdLeft,
                    geometry);
            })
            .ToArray();

        return boundaries;
    }

    private static IGeometry ApplyRotation(IGeometry geometry, Quaterniond rotation)
    {
        return geometry switch
        {
            Point3 point => Rotate(point, rotation),
            Segment3 segment => Rotate(segment, rotation),
            Polyline3 polyline => Rotate(polyline, rotation),
            _ => geometry
        };
    }

    private static Point3 Rotate(Point3 point, Quaterniond rotation)
    {
        if (point.IsEmpty)
            return point;

        var v = RotateVector(new Vector3d(point.X, point.Y, point.Z), rotation);
        return new Point3(v.X, v.Y, v.Z);
    }

    private static Segment3 Rotate(Segment3 segment, Quaterniond rotation)
    {
        if (segment.IsEmpty)
            return segment;

        return new Segment3(Rotate(segment.Start, rotation), Rotate(segment.End, rotation));
    }

    private static Polyline3 Rotate(Polyline3 polyline, Quaterniond rotation)
    {
        if (polyline.IsEmpty)
            return polyline;

        var points = new Point3[polyline.PointCount];
        for (var i = 0; i < points.Length; i++)
        {
            points[i] = Rotate(polyline[i], rotation);
        }

        return new Polyline3(points);
    }

    private static Vector3d RotateVector(Vector3d vector, Quaterniond rotation)
    {
        var v = new Quaterniond(vector.X, vector.Y, vector.Z, 0d);
        var inv = Inverse(rotation);
        var rotated = Multiply(Multiply(rotation, v), inv);
        return new Vector3d(rotated.X, rotated.Y, rotated.Z);
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
