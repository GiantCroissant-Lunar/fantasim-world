using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;

/// <summary>
/// Optional event associating a named motion model with a plate.
/// </summary>
[UnifyModel]
public readonly record struct PlateMotionModelAssignedEvent(
    [property: UnifyProperty(0)] Guid EventId,
    [property: UnifyProperty(1)] PlateId PlateId,
    [property: UnifyProperty(2)] string ModelId,
    [property: UnifyProperty(3)] CanonicalTick Tick,
    [property: UnifyProperty(4)] long Sequence,
    [property: UnifyProperty(5)] TruthStreamIdentity StreamIdentity,
    [property: UnifyProperty(6)] ReadOnlyMemory<byte> PreviousHash,
    [property: UnifyProperty(7)] ReadOnlyMemory<byte> Hash
) : IPlateKinematicsEvent
{
    string IPlateKinematicsEvent.EventType => nameof(PlateMotionModelAssignedEvent);
}
