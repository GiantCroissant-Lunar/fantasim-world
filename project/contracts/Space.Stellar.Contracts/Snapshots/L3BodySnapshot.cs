using FantaSim.Space.Stellar.Contracts.Entities;
using FantaSim.Space.Stellar.Contracts.Mechanics;
using UnifySerialization.Abstractions;

namespace FantaSim.Space.Stellar.Contracts.Snapshots;

[UnifyModel]
public readonly record struct L3BodySnapshot(
    [property: UnifyProperty(0)] Guid BodyId,
    [property: UnifyProperty(1)] BodyType Type,
    [property: UnifyProperty(2)] OrbitalElements? Orbit,
    [property: UnifyProperty(3)] double MassKg,
    [property: UnifyProperty(4)] double RadiusM,
    [property: UnifyProperty(5)] Guid? ParentId,
    [property: UnifyProperty(6)] double ParentMassKg
);
