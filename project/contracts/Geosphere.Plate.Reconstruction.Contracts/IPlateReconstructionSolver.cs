using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Topology.Contracts.Derived;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts;

/// <summary>
/// Solver for reconstructing plate boundaries at a target time.
/// </summary>
public interface IPlateReconstructionSolver
{
    /// <summary>
    /// Reconstructs plate boundaries at the specified target tick using the given policy.
    /// </summary>
    /// <param name="topology">Current topology state view.</param>
    /// <param name="kinematics">Current kinematics state view.</param>
    /// <param name="policy">
    /// Unified reconstruction policy per RFC-V2-0045 §3.1.
    /// Replaces scattered configuration with a single policy object.
    /// </param>
    /// <param name="targetTick">The canonical tick to reconstruct boundaries at.</param>
    /// <param name="options">
    /// Legacy reconstruction options for backward compatibility.
    /// Prefer using <paramref name="policy"/> for new code.
    /// </param>
    /// <returns>List of reconstructed boundaries.</returns>
    IReadOnlyList<ReconstructedBoundary> ReconstructBoundaries(
        IPlateTopologyStateView topology,
        IPlateKinematicsStateView kinematics,
        ReconstructionPolicy policy,
        CanonicalTick targetTick,
        ReconstructionOptions? options = null);
}
