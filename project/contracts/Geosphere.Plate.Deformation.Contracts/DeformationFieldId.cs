namespace FantaSim.Geosphere.Plate.Deformation.Contracts;

public static class DeformationFieldId
{
    /// <summary>Dilatation rate (trace of strain-rate tensor).</summary>
    public const string DilatationRate = "deformation.dilatation_rate";

    /// <summary>Second invariant of strain-rate tensor.</summary>
    public const string SecondInvariant = "deformation.second_invariant";

    /// <summary>Vorticity (scalar, 2D surface).</summary>
    public const string Vorticity = "deformation.vorticity";

    /// <summary>Divergence (positive dilatation rate only, zero where negative).</summary>
    public const string Divergence = "deformation.divergence";

    /// <summary>Convergence (absolute value of negative dilatation rate, zero where positive).</summary>
    public const string Convergence = "deformation.convergence";
}
