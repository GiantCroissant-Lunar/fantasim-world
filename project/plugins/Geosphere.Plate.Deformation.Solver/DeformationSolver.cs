using System;
using FantaSim.Geosphere.Plate.Deformation.Contracts;
using FantaSim.Geosphere.Plate.Reconstruction.Contracts.Policies;
using FantaSim.Geosphere.Plate.Sampling.Contracts;
using Plate.TimeDete.Time.Primitives;

namespace FantaSim.Geosphere.Plate.Deformation.Solver;

public sealed class DeformationSolver : IDeformationService
{
    public StrainRateCoverage ComputeStrainRate(
        SamplingDomain domain,
        CanonicalTick tick,
        ReconstructionPolicy policy)
    {
        // TODO: Implement numerical differentiation per RFC-V2-0054 Section 6.
        throw new NotImplementedException("TODO: Implement numerical differentiation per RFC-V2-0054 Section 6");
    }

    public ScalarCoverage ComputeScalarField(
        SamplingDomain domain,
        string deformationFieldId,
        CanonicalTick tick,
        ReconstructionPolicy policy)
    {
        // TODO: Implement scalar invariants per RFC-V2-0054 Sections 4-5.
        throw new NotImplementedException("TODO: Implement scalar invariants per RFC-V2-0054 Sections 4-5");
    }
}
