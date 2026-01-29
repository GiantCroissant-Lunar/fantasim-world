using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Reconstruction.Solver;

/// <summary>
/// Minimal reconstruction solver implementation for RFC-V2-0024.
/// </summary>
/// <remarks>
/// <para>
/// <b>Geometry rotation:</b> Applies quaternion rotation to all 3D geometry types
/// (Point3, Segment3, Polyline3). 2D geometry types are returned unchanged.
/// </para>
/// <para>
/// <b>Topology assumptions:</b> Expects topology slice boundaries at the target tick
/// (topology already time-cutoff materialized by caller). Retired boundaries are excluded.
/// </para>
/// <para>
/// <b>Provenance policy:</b> Attaches single-plate provenance (left plate) per boundary.
/// </para>
/// <para>
/// <b>Kinematics fallback policy:</b> If <c>kinematics.TryGetRotation()</c> returns false
/// for a given plate at the target tick, the solver uses <see cref="Quaterniond.Identity"/>
/// (no rotation). This means geometry is returned unchanged when kinematics data is missing.
/// This behavior is deterministic and suitable for Solver Lab verification.
/// </para>
/// <para>
/// <b>Determinism:</b> Output is ordered by BoundaryId/FeatureId using RFC4122 byte ordering
/// for reproducible results across runs.
/// </para>
/// </remarks>
public sealed class NaivePlateReconstructionSolver : IPlateReconstructionSolver, IPlateFeatureReconstructionSolver
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

    public IReadOnlyList<ReconstructedFeature> ReconstructFeatures(
        IReadOnlyList<ReconstructableFeature> features,
        IPlateKinematicsStateView kinematics,
        CanonicalTick targetTick,
        ReconstructionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(kinematics);

        var reconstructed = features
            .Where(f => f.PlateIdProvenance.HasValue)
            .OrderBy(f => f.FeatureId.Value, GuidOrdering.Rfc4122Comparer)
            .Select(f =>
            {
                var plateId = f.PlateIdProvenance!.Value;

                var rotation = Quaterniond.Identity;
                if (kinematics.TryGetRotation(plateId, targetTick, out var r))
                    rotation = r;

                var geometry = ApplyRotation(f.Geometry, rotation);

                return new ReconstructedFeature(
                    f.FeatureId,
                    plateId,
                    geometry);
            })
            .ToArray();

        return reconstructed;
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
        var inv = rotation.Inverse();
        var rotated = Quaterniond.Multiply(Quaterniond.Multiply(rotation, v), inv);
        return new Vector3d(rotated.X, rotated.Y, rotated.Z);
    }
}
