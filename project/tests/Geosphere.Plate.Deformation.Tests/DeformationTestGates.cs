using System;
using FantaSim.Geosphere.Plate.Deformation.Contracts;
using FantaSim.Geosphere.Plate.Deformation.Solver;
using Xunit;

namespace FantaSim.Geosphere.Plate.Deformation.Tests;

public sealed class DeformationTestGates
{
    [Fact(Skip = "Pending full DeformationSolver implementation")]
    public void RigidRotation_ZeroStrainRate() // RFC-V2-0054 §10.1
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "Pending full DeformationSolver implementation")]
    public void PureDivergence_PositiveDilatation_ZeroVorticity() // RFC-V2-0054 §10.2
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "Pending full DeformationSolver implementation")]
    public void SecondInvariant_AlwaysNonNegative() // RFC-V2-0054 §10.3
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "Pending full DeformationSolver implementation")]
    public void DivergenceConvergence_PartitionsDilatation() // RFC-V2-0054 §10.4
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "Pending full DeformationSolver implementation")]
    public void StrainRate_NaN_AtPoles() // RFC-V2-0054 §10.5
    {
        throw new NotImplementedException();
    }
}
