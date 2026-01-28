using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Output;

namespace FantaSim.Geosphere.Plate.Reconstruction.Contracts;

public interface IPlateFeatureReconstructionSolver
{
    IReadOnlyList<ReconstructedFeature> ReconstructFeatures(
        IReadOnlyList<ReconstructableFeature> features,
        IPlateKinematicsStateView kinematics,
        CanonicalTick targetTick,
        ReconstructionOptions? options = null);
}
