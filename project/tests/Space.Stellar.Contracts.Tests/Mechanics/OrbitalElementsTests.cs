using FantaSim.Space.Stellar.Contracts.Mechanics;

namespace FantaSim.Space.Stellar.Contracts.Tests.Mechanics;

public sealed class OrbitalElementsTests
{
    [Fact]
    public void IsValid_WhenSemiMajorAxisIsNonPositive_ReturnsFalse()
    {
        var orbit = ValidOrbit() with { SemiMajorAxisM = 0 };
        Assert.False(orbit.IsValid());
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.0)]
    [InlineData(1.1)]
    public void IsValid_WhenEccentricityOutOfRange_ReturnsFalse(double eccentricity)
    {
        var orbit = ValidOrbit() with { Eccentricity = eccentricity };
        Assert.False(orbit.IsValid());
    }

    [Theory]
    [InlineData(-1e-6)]
    [InlineData(3.141592653589793 + 1e-6)]
    public void IsValid_WhenInclinationOutOfRange_ReturnsFalse(double inclinationRad)
    {
        var orbit = ValidOrbit() with { InclinationRad = inclinationRad };
        Assert.False(orbit.IsValid());
    }

    private static OrbitalElements ValidOrbit() => new(
        SemiMajorAxisM: 1.0,
        Eccentricity: 0.0,
        InclinationRad: 0.0,
        LongitudeOfAscendingNodeRad: 0.0,
        ArgumentOfPeriapsisRad: 0.0,
        MeanAnomalyAtEpochRad: 0.0,
        EpochTimeS: 0.0);
}
