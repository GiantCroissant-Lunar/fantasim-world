using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;

/// <summary>
/// Retires a previously-defined motion segment.
/// </summary>
[UnifyModel]
public readonly record struct MotionSegmentRetiredEvent(
    [property: UnifyProperty(0)] Guid EventId,
    [property: UnifyProperty(1)] PlateId PlateId,
    [property: UnifyProperty(2)] MotionSegmentId SegmentId,
    [property: UnifyProperty(3)] string Reason,
    [property: UnifyProperty(4)] CanonicalTick Tick,
    [property: UnifyProperty(5)] long Sequence,
    [property: UnifyProperty(6)] TruthStreamIdentity StreamIdentity,
    [property: UnifyProperty(7)] ReadOnlyMemory<byte> PreviousHash,
    [property: UnifyProperty(8)] ReadOnlyMemory<byte> Hash
) : IPlateKinematicsEvent
{
    string IPlateKinematicsEvent.EventType => nameof(MotionSegmentRetiredEvent);
}
