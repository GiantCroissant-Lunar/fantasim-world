using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts;

/// <summary>
/// Solves plate feature reconstruction by transforming features through time.
/// </summary>
public interface IPlateFeatureReconstructionSolver
{
    /// <summary>
    /// Reconstructs features to their positions at the target tick.
    /// </summary>
    /// <param name="features">The features to reconstruct.</param>
    /// <param name="kinematics">The plate kinematics state providing rotation data.</param>
    /// <param name="policy">The reconstruction policy controlling behavior. See RFC-V2-0045 §3.1.</param>
    /// <param name="targetTick">The target time tick for reconstruction.</param>
    /// <param name="options">Optional reconstruction options.</param>
    /// <returns>The reconstructed features at the target tick.</returns>
    /// <remarks>
    /// The policy parameter determines how reconstruction handles edge cases such as
    /// missing rotation data or features crossing plate boundaries.
    /// </remarks>
    IReadOnlyList<ReconstructedFeature> ReconstructFeatures(
        IReadOnlyList<ReconstructableFeature> features,
        IPlateKinematicsStateView kinematics,
        ReconstructionPolicy policy,
        CanonicalTick targetTick,
        ReconstructionOptions? options = null);
}
