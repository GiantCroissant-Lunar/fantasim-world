using UnifySerialization.Abstractions;

namespace FantaSim.Space.Stellar.Contracts.Entities;

[UnifyModel]
public readonly record struct PlanetProperties(
    [property: UnifyProperty(0)] double MassKg,
    [property: UnifyProperty(1)] double EquatorialRadiusM,
    [property: UnifyProperty(2)] double PolarRadiusM,
    [property: UnifyProperty(3)] double ObliquityRad,
    [property: UnifyProperty(4)] double RotationPeriodS,
    [property: UnifyProperty(5)] bool ProgradeRotation,
    [property: UnifyProperty(6)] double BondAlbedo,
    [property: UnifyProperty(7)] PlanetClass Class
) : IBodyProperties;
