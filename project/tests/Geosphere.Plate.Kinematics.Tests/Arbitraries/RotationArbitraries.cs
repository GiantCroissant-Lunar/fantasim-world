using FsCheck;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Kinematics.Tests.Arbitraries;

/// <summary>
/// Custom FsCheck Arbitrary generators for kinematics numeric types.
/// </summary>
public static class RotationArbitraries
{
    /// <summary>
    /// Generates arbitrary normalized 3D vectors (unit vectors) for rotation axes.
    /// </summary>
    public static Arbitrary<Vector3d> UnitVector()
    {
        // Generate random vectors and normalize them
        return Arb.Default.Double()
            .Generator
            .Three()
            .Select(t => new Vector3d(t.Item1, t.Item2, t.Item3).Normalize())
            .ToArbitrary();
    }

    /// <summary>
    /// Generates arbitrary rotation angles in radians, constrained to reasonable range.
    /// Range: -2π to +2π to cover multiple rotations while avoiding excessive winding.
    /// </summary>
    public static Arbitrary<double> RotationAngle()
    {
        return Gen.Choose(-628, 628)  // -2π*100 to 2π*100 in hundredths of radians
            .Select(x => x / 100.0)    // Convert to radians
            .ToArbitrary();
    }

    /// <summary>
    /// Generates arbitrary FiniteRotation values using random axis and angle.
    /// </summary>
    public static Arbitrary<FiniteRotation> FiniteRotation()
    {
        return UnitVector().Generator
            .SelectMany(axis =>
                RotationAngle().Generator.Select(angle =>
                    FiniteRotation.FromAxisAngle(axis, angle)))
            .ToArbitrary();
    }

    /// <summary>
    /// Generates arbitrary quaternions (not necessarily normalized).
    /// Used for testing normalization and stability.
    /// </summary>
    public static Arbitrary<Quaterniond> Quaternion()
    {
        return Arb.Default.Double()
            .Generator
            .Four()
            .Select(t => new Quaterniond(t.Item1, t.Item2, t.Item3, t.Item4))
            .ToArbitrary();
    }
}
