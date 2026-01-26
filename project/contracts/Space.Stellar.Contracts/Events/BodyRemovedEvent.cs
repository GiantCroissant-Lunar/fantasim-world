using UnifySerialization.Abstractions;

namespace FantaSim.Space.Stellar.Contracts.Events;

[UnifyModel]
public readonly record struct BodyRemovedEvent(
    [property: UnifyProperty(0)] Guid SystemId,
    [property: UnifyProperty(1)] Guid BodyId,
    [property: UnifyProperty(2)] BodyRemovalReason Reason,
    [property: UnifyProperty(3)] long EventSequence
);
