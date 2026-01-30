using System.Runtime.InteropServices;
using FantaSim.Space.Stellar.Contracts.Mechanics;
using UnifySerialization.Abstractions;

namespace FantaSim.Space.Stellar.Contracts.Events;

[StructLayout(LayoutKind.Auto)]
[UnifyModel]
public readonly record struct OrbitChangedEvent(
    [property: UnifyProperty(0)] Guid SystemId,
    [property: UnifyProperty(1)] Guid BodyId,
    [property: UnifyProperty(2)] OrbitalElements OldOrbit,
    [property: UnifyProperty(3)] OrbitalElements NewOrbit,
    [property: UnifyProperty(4)] OrbitChangeReason Reason,
    [property: UnifyProperty(5)] long EventSequence
);
