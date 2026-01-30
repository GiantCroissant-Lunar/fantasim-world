using System.Runtime.InteropServices;
using UnifySerialization.Abstractions;

namespace FantaSim.Space.Stellar.Contracts.Entities;

[StructLayout(LayoutKind.Auto)]
[UnifyModel]
public readonly record struct MoonProperties(
    [property: UnifyProperty(0)] double MassKg,
    [property: UnifyProperty(1)] double RadiusM,
    [property: UnifyProperty(2)] bool TidallyLocked,
    [property: UnifyProperty(3)] double Albedo
) : IBodyProperties;
