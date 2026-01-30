using System.Runtime.InteropServices;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Kinematics.Contracts.Events;

/// <summary>
/// Defines or updates a motion segment for a plate over an interval [TickA, TickB].
/// </summary>
[StructLayout(LayoutKind.Auto)]
[UnifyModel]
public readonly record struct MotionSegmentUpsertedEvent(
    [property: UnifyProperty(0)] Guid EventId,
    [property: UnifyProperty(1)] PlateId PlateId,
    [property: UnifyProperty(2)] MotionSegmentId SegmentId,
    [property: UnifyProperty(3)] CanonicalTick TickA,
    [property: UnifyProperty(4)] CanonicalTick TickB,
    [property: UnifyProperty(5)] QuantizedEulerPoleRotation StageRotation,
    [property: UnifyProperty(6)] CanonicalTick Tick,
    [property: UnifyProperty(7)] long Sequence,
    [property: UnifyProperty(8)] TruthStreamIdentity StreamIdentity,
    [property: UnifyProperty(9)] ReadOnlyMemory<byte> PreviousHash,
    [property: UnifyProperty(10)] ReadOnlyMemory<byte> Hash
) : IPlateKinematicsEvent
{
    string IPlateKinematicsEvent.EventType => nameof(MotionSegmentUpsertedEvent);
}
