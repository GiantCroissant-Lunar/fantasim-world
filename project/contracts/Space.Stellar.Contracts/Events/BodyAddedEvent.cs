using FantaSim.Space.Stellar.Contracts.Entities;
using FantaSim.Space.Stellar.Contracts.Mechanics;
using UnifySerialization.Abstractions;

namespace FantaSim.Space.Stellar.Contracts.Events;

[UnifyModel]
public readonly record struct BodyAddedEvent(
    [property: UnifyProperty(0)] Guid SystemId,
    [property: UnifyProperty(1)] Guid BodyId,
    [property: UnifyProperty(2)] Guid? ParentBodyId,
    [property: UnifyProperty(3)] BodyType Type,
    [property: UnifyProperty(4)] string Name,
    [property: UnifyProperty(5)] OrbitalElements? Orbit,
    [property: UnifyProperty(6)] byte[] PropertiesData,
    [property: UnifyProperty(7)] long EventSequence
);
