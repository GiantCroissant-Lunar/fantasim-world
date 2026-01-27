using FantaSim.Space.Stellar.Contracts.Mechanics;

namespace FantaSim.Space.Stellar.Solvers.Reference.Tests.Orbit;

public class ReferenceOrbitSolverTests
{
    [Fact]
    public void GetOrbitalPeriod_CalculatesCorrectPeriodForEarth()
    {
        // Earth orbit: semi-major axis ~149.6e9 m, Sun mass ~1.989e30 kg
        double semiMajorAxis = 149.6e9; // meters
        double centralMass = 1.989e30; // kg

        double period = OrbitalMechanics.GetOrbitalPeriod(semiMajorAxis, centralMass);
        double expectedPeriod = 365.25 * 24 * 3600; // ~1 year in seconds

        // Allow 1% tolerance due to rounding
        Assert.Equal(expectedPeriod, period, expectedPeriod * 0.01);
    }

    [Fact]
    public void GetOrbitalPeriod_ThrowsForNegativeSemiMajorAxis()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrbitalMechanics.GetOrbitalPeriod(-1.0, 1.989e30));
    }

    [Fact]
    public void GetOrbitalPeriod_ThrowsForNegativeCentralMass()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrbitalMechanics.GetOrbitalPeriod(149.6e9, -1.0));
    }

    [Fact]
    public void GetMeanMotion_CalculatesCorrectMeanMotion()
    {
        double semiMajorAxis = 149.6e9; // meters
        double centralMass = 1.989e30; // kg

        double meanMotion = OrbitalMechanics.GetMeanMotion(semiMajorAxis, centralMass);
        double period = OrbitalMechanics.GetOrbitalPeriod(semiMajorAxis, centralMass);
        double expectedMeanMotion = 2.0 * Math.PI / period;

        Assert.Equal(expectedMeanMotion, meanMotion, expectedMeanMotion * 0.0001);
    }
}
