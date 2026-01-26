using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

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
            .Select(b => new ReconstructedBoundary(
                b.BoundaryId,
                b.PlateIdLeft,
                b.Geometry))
            .ToArray();

        return boundaries;
    }
}
