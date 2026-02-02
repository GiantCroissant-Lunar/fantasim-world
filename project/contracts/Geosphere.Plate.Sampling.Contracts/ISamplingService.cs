using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;

namespace FantaSim.Geosphere.Plate.Sampling.Contracts;

public interface ISamplingService
{
    /// <summary>
    /// Sample a scalar field over the given domain at a specific tick.
    /// </summary>
    ScalarCoverage SampleScalarField(
        SamplingDomain domain,
        ScalarFieldId fieldId,
        CanonicalTick tick,
        ReconstructionPolicy policy);

    /// <summary>
    /// Sample a vector field over the given domain at a specific tick.
    /// </summary>
    VectorCoverage SampleVectorField(
        SamplingDomain domain,
        VectorFieldId fieldId,
        CanonicalTick tick,
        ReconstructionPolicy policy);
}
