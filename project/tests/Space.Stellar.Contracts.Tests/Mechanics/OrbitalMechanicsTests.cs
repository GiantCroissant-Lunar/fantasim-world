using FantaSim.Space.Stellar.Contracts.Constants;
using FantaSim.Space.Stellar.Contracts.Mechanics;

namespace FantaSim.Space.Stellar.Contracts.Tests.Mechanics;

public sealed class OrbitalMechanicsTests
{
    [Fact]
    public void OrbitalPeriod_EarthLikeOrbit_ReturnsOneYear()
    {
        double periodS = OrbitalMechanics.GetOrbitalPeriod(
            AstronomicalConstants.AU_Meters,
            AstronomicalConstants.SolarMass_Kg);

        Assert.InRange(
            periodS,
            AstronomicalConstants.SecondsPerYear * 0.99,
            AstronomicalConstants.SecondsPerYear * 1.01);
    }
}
