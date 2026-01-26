using UnifySerialization.Abstractions;

namespace FantaSim.Space.Stellar.Contracts.Events;

[UnifyModel]
public readonly record struct SystemCreatedEvent(
    [property: UnifyProperty(0)] Guid SystemId,
    [property: UnifyProperty(1)] string SystemName,
    [property: UnifyProperty(2)] double EpochTimeS,
    [property: UnifyProperty(3)] long EventSequence,
    [property: UnifyProperty(4)] DateTime CreatedAtUtc
);
