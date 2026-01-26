using FantaSim.Space.Stellar.Contracts.Entities;
using FantaSim.Space.Stellar.Contracts.Mechanics;
using FantaSim.Space.Stellar.Contracts.Topology;
using FantaSim.Space.Stellar.Contracts.Validation;

namespace FantaSim.Space.Stellar.Contracts.Tests.Validation;

public sealed class L3ValidationRulesTests
{
    [Fact]
    public void ValidateTopology_WhenRootHasOrbit_ReturnsInvalid()
    {
        var rootId = Guid.NewGuid();
        var root = new L3Body
        {
            BodyId = rootId,
            Name = "Sol",
            Type = BodyType.Star,
            Orbit = ValidOrbit(),
            ParentId = null,
            Properties = new StarProperties(
                MassKg: 1,
                RadiusM: 1,
                LuminosityW: 1,
                EffectiveTemperatureK: 1,
                SpectralClass: "G2V",
                AgeYears: 1,
                Metallicity: 0),
            Children = Array.Empty<L3Body>(),
        };

        var topology = new L3SystemTopology
        {
            SystemId = Guid.NewGuid(),
            SystemName = "Test",
            EpochTimeS = 0,
            RootBody = root,
        };

        var result = L3ValidationRules.ValidateTopology(topology);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("must not have an orbit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateTopology_WhenChildHasParentButNoOrbit_ReturnsInvalid()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var child = new L3Body
        {
            BodyId = childId,
            Name = "Earth",
            Type = BodyType.Planet,
            Orbit = null,
            ParentId = rootId,
            Properties = new PlanetProperties(
                MassKg: 1,
                EquatorialRadiusM: 1,
                PolarRadiusM: 1,
                ObliquityRad: 0,
                RotationPeriodS: 1,
                ProgradeRotation: true,
                BondAlbedo: 0,
                Class: PlanetClass.Terrestrial),
            Children = Array.Empty<L3Body>(),
        };

        var root = new L3Body
        {
            BodyId = rootId,
            Name = "Sol",
            Type = BodyType.Star,
            Orbit = null,
            ParentId = null,
            Properties = new StarProperties(
                MassKg: 1,
                RadiusM: 1,
                LuminosityW: 1,
                EffectiveTemperatureK: 1,
                SpectralClass: "G2V",
                AgeYears: 1,
                Metallicity: 0),
            Children = new[] { child },
        };

        var topology = new L3SystemTopology
        {
            SystemId = Guid.NewGuid(),
            SystemName = "Test",
            EpochTimeS = 0,
            RootBody = root,
        };

        var result = L3ValidationRules.ValidateTopology(topology);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("has a parent but no orbit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateTopology_WhenCycleInParentChain_ReturnsInvalid()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var child = new L3Body
        {
            BodyId = childId,
            Name = "Earth",
            Type = BodyType.Planet,
            Orbit = ValidOrbit(),
            ParentId = rootId,
            Properties = new PlanetProperties(
                MassKg: 1,
                EquatorialRadiusM: 1,
                PolarRadiusM: 1,
                ObliquityRad: 0,
                RotationPeriodS: 1,
                ProgradeRotation: true,
                BondAlbedo: 0,
                Class: PlanetClass.Terrestrial),
            Children = Array.Empty<L3Body>(),
        };

        // Create an invalid cycle: root -> child, and root.ParentId points back to child.
        var root = new L3Body
        {
            BodyId = rootId,
            Name = "Sol",
            Type = BodyType.Star,
            Orbit = null,
            ParentId = childId,
            Properties = new StarProperties(
                MassKg: 1,
                RadiusM: 1,
                LuminosityW: 1,
                EffectiveTemperatureK: 1,
                SpectralClass: "G2V",
                AgeYears: 1,
                Metallicity: 0),
            Children = new[] { child },
        };

        var topology = new L3SystemTopology
        {
            SystemId = Guid.NewGuid(),
            SystemName = "Test",
            EpochTimeS = 0,
            RootBody = root,
        };

        var result = L3ValidationRules.ValidateTopology(topology);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Cycle detected", StringComparison.OrdinalIgnoreCase));
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
