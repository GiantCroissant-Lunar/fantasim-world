using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Sampling.Contracts;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Deformation.Contracts;

public interface IDeformationService
{
    /// <summary>
    /// Compute strain-rate tensors over the given sampling domain.
    /// </summary>
    StrainRateCoverage ComputeStrainRate(
        SamplingDomain domain,
        CanonicalTick tick,
        ReconstructionPolicy policy);

    /// <summary>
    /// Compute a scalar deformation field (dilatation, second invariant, vorticity, etc.).
    /// </summary>
    ScalarCoverage ComputeScalarField(
        SamplingDomain domain,
        string deformationFieldId,
        CanonicalTick tick,
        ReconstructionPolicy policy);
}
