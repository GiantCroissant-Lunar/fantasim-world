using FantaSim.Space.Stellar.Contracts.Constants;
using FantaSim.Space.Stellar.Solvers.Reference;

namespace FantaSim.Space.Stellar.Solvers.Reference.Tests.Insolation;

public sealed class ReferenceInsolationSolverTests
{
    private readonly ReferenceInsolationSolver _solver = new();

    [Fact]
    public void SolarFlux_AtOneAU_MatchesSolarConstant()
    {
        double flux = _solver.CalculateSolarFlux(
            starLuminosityW: AstronomicalConstants.SolarLuminosity_W,
            distanceM: AstronomicalConstants.AU_Meters);

        Assert.True(
            Math.Abs(flux - AstronomicalConstants.SolarConstant_WPerM2) <= 1.0,
            $"Expected flux {flux} to be within ±1 of {AstronomicalConstants.SolarConstant_WPerM2}.");
    }

    [Fact]
    public void PolarNight_Lat80_DecMinus23p44_DailyInsolationIsZero()
    {
        double lat = 80.0 * AstronomicalConstants.DegreesToRadians;
        double dec = -23.44 * AstronomicalConstants.DegreesToRadians;

        double q = _solver.CalculateDailyInsolation(lat, dec, AstronomicalConstants.SolarConstant_WPerM2);

        Assert.Equal(0.0, q);
    }

    [Fact]
    public void PolarDay_Lat80_DecPlus23p44_DayLengthIs24Hours()
    {
        double lat = 80.0 * AstronomicalConstants.DegreesToRadians;
        double dec = 23.44 * AstronomicalConstants.DegreesToRadians;

        double hours = _solver.CalculateDayLength(lat, dec);

        AssertCloseAbsolute(expected: 24.0, actual: hours, absTol: 1e-9);
    }

    [Fact]
    public void EquatorAtEquinox_DailyInsolation_IsS0OverPi_Within2Percent()
    {
        double lat = 0.0;
        double dec = 0.0;
        double s0 = AstronomicalConstants.SolarConstant_WPerM2;

        double q = _solver.CalculateDailyInsolation(lat, dec, s0);
        double expected = s0 / Math.PI;

        AssertCloseRelative(expected, q, relTol: 0.02);
    }

    [Fact]
    public void NegativeDistanceForFlux_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _solver.CalculateSolarFlux(
            starLuminosityW: AstronomicalConstants.SolarLuminosity_W,
            distanceM: -1.0));
    }

    [Fact]
    public void LatitudeOutsideRange_Throws()
    {
        double invalidLat = 91.0 * AstronomicalConstants.DegreesToRadians;

        Assert.Throws<ArgumentOutOfRangeException>(() => _solver.CalculateDailyInsolation(
            latitudeRad: invalidLat,
            declinationRad: 0.0,
            solarConstant: AstronomicalConstants.SolarConstant_WPerM2));
    }

    [Fact]
    public void NegativeSolarConstant_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _solver.CalculateDailyInsolation(
            latitudeRad: 0.0,
            declinationRad: 0.0,
            solarConstant: -1.0));
    }

    private static void AssertCloseAbsolute(double expected, double actual, double absTol)
    {
        if (double.IsNaN(actual) || double.IsInfinity(actual))
            Assert.Fail($"Actual value must be finite, got {actual}.");

        double diff = Math.Abs(actual - expected);
        Assert.True(diff <= absTol, $"Expected {actual} to be within {absTol} of {expected}. Diff={diff}.");
    }

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
