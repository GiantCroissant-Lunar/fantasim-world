using FantaSim.Space.Stellar.Contracts.Constants;
using FantaSim.Space.Stellar.Contracts.Mechanics;
using FantaSim.Space.Stellar.Solvers.Reference;

namespace FantaSim.Space.Stellar.Solvers.Reference.Tests.Orbit;

public sealed class ReferenceOrbitSolverTests
{
    private readonly ReferenceOrbitSolver _solver = new();

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    public void CircularOrbit_RadiusEqualsSemiMajorAxis(double periodFraction)
    {
        var orbit = CircularOrbit();
        double centralMassKg = AstronomicalConstants.SolarMass_Kg;

        double periodS = OrbitalMechanics.GetOrbitalPeriod(orbit.SemiMajorAxisM, centralMassKg);
        double timeS = periodFraction * periodS;

        var state = _solver.CalculateOrbitalState(orbit, centralMassKg, timeS);

        AssertCloseRelative(orbit.SemiMajorAxisM, state.DistanceM, relTol: 1e-9);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    public void CircularOrbit_SpeedEqualsCircularVelocity(double periodFraction)
    {
        var orbit = CircularOrbit();
        double centralMassKg = AstronomicalConstants.SolarMass_Kg;

        double periodS = OrbitalMechanics.GetOrbitalPeriod(orbit.SemiMajorAxisM, centralMassKg);
        double timeS = periodFraction * periodS;

        var state = _solver.CalculateOrbitalState(orbit, centralMassKg, timeS);
        double expectedSpeed = OrbitalMechanics.GetOrbitalVelocity(
            distanceM: orbit.SemiMajorAxisM,
            semiMajorAxisM: orbit.SemiMajorAxisM,
            centralMassKg: centralMassKg);

        AssertCloseRelative(expectedSpeed, state.SpeedMPerS, relTol: 1e-9);
    }

    [Fact]
    public void EarthLikeOrbit_PeriodIsOneYear()
    {
        var orbit = EarthLikeOrbit();
        double centralMassKg = AstronomicalConstants.SolarMass_Kg;

        // Use afterTimeS > 0 so the solver returns the next occurrence.
        double timeAtPeriapsisS = _solver.FindTimeAtTrueAnomaly(
            orbit,
            centralMassKg,
            targetTrueAnomalyRad: 0.0,
            afterTimeS: 1.0);

        AssertCloseRelative(AstronomicalConstants.SecondsPerYear, timeAtPeriapsisS, relTol: 0.01);
    }

    [Fact]
    public void FindTimeAtTrueAnomaly_ReturnsNextOccurrence()
    {
        var orbit = EarthLikeOrbit();
        double centralMassKg = AstronomicalConstants.SolarMass_Kg;
        double targetNu = 90.0 * AstronomicalConstants.DegreesToRadians;

        double t1 = _solver.FindTimeAtTrueAnomaly(orbit, centralMassKg, targetNu, afterTimeS: 0.0);
        double t2 = _solver.FindTimeAtTrueAnomaly(orbit, centralMassKg, targetNu, afterTimeS: t1 + 1.0);

        Assert.True(t2 > t1);
        AssertCloseRelative(AstronomicalConstants.SecondsPerYear, t2 - t1, relTol: 0.01);
    }

    [Fact]
    public void InvalidCentralMass_Throws()
    {
        var orbit = EarthLikeOrbit();

        Assert.Throws<ArgumentOutOfRangeException>(() => _solver.CalculatePosition(orbit, centralMassKg: 0.0, timeS: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _solver.FindTimeAtTrueAnomaly(orbit, centralMassKg: -1.0, targetTrueAnomalyRad: 0.0, afterTimeS: 0.0));
    }

    [Fact]
    public void InvalidOrbit_Throws()
    {
        var valid = EarthLikeOrbit();

        var invalidA = valid with { SemiMajorAxisM = 0.0 };
        Assert.Throws<ArgumentOutOfRangeException>(() => _solver.CalculateVelocity(invalidA, AstronomicalConstants.SolarMass_Kg, timeS: 0.0));

        var invalidE = valid with { Eccentricity = 1.0 };
        Assert.Throws<ArgumentOutOfRangeException>(() => _solver.CalculateVelocity(invalidE, AstronomicalConstants.SolarMass_Kg, timeS: 0.0));
    }

    private static OrbitalElements CircularOrbit() => new(
        SemiMajorAxisM: AstronomicalConstants.AU_Meters,
        Eccentricity: 0.0,
        InclinationRad: 0.0,
        LongitudeOfAscendingNodeRad: 0.0,
        ArgumentOfPeriapsisRad: 0.0,
        MeanAnomalyAtEpochRad: 0.0,
        EpochTimeS: 0.0);

    private static OrbitalElements EarthLikeOrbit() => new(
        SemiMajorAxisM: AstronomicalConstants.AU_Meters,
        Eccentricity: 0.0167,
        InclinationRad: 0.0,
        LongitudeOfAscendingNodeRad: 0.0,
        ArgumentOfPeriapsisRad: 0.0,
        MeanAnomalyAtEpochRad: 0.0,
        EpochTimeS: 0.0);

    private static void AssertCloseRelative(double expected, double actual, double relTol)
    {
        if (double.IsNaN(actual) || double.IsInfinity(actual))
            Assert.Fail($"Actual value must be finite, got {actual}.");

        double denom = Math.Max(1.0, Math.Abs(expected));
        double diff = Math.Abs(actual - expected);
        Assert.True(
            diff <= relTol * denom,
            $"Expected {actual} to be within {relTol:P2} of {expected}. Diff={diff}.");
    }
}
