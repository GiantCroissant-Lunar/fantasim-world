using UnifySerialization.Abstractions;

namespace FantaSim.Space.Stellar.Contracts.Entities;

[UnifyModel]
public readonly record struct StarProperties(
    [property: UnifyProperty(0)] double MassKg,
    [property: UnifyProperty(1)] double RadiusM,
    [property: UnifyProperty(2)] double LuminosityW,
    [property: UnifyProperty(3)] double EffectiveTemperatureK,
    [property: UnifyProperty(4)] string SpectralClass,
    [property: UnifyProperty(5)] double AgeYears,
    [property: UnifyProperty(6)] double Metallicity
) : IBodyProperties;
