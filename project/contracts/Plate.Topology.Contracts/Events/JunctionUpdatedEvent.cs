using System;
using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Contracts.Events;

/// <summary>
/// Event representing an update to a junction per FR-016.
///
/// Junction updates capture changes to which boundaries meet at a junction
/// and/or changes to the junction's spatial location. Per FR-016, junction
/// updates must be explicit when connected boundaries change.
/// </summary>
/// <param name="EventId">Unique identifier for this event (UUIDv7).</param>
/// <param name="JunctionId">The unique identifier of the junction being updated.</param>
/// <param name="NewBoundaryIds">The updated list of boundaries meeting at this junction.</param>
/// <param name="NewLocation">The new location of this junction (optional).</param>
/// <param name="Tick">Canonical simulation tick when this event occurred.</param>
/// <param name="Sequence">Ordering within the event stream.</param>
/// <param name="StreamIdentity">The truth stream this event belongs to.</param>
/// <param name="PreviousHash">Hash of the previous event in the chain (empty for genesis).</param>
/// <param name="Hash">Cryptographic hash of this event (computed, not set by callers).</param>
public readonly record struct JunctionUpdatedEvent(
    Guid EventId,
    JunctionId JunctionId,
    BoundaryId[] NewBoundaryIds,
    Point2D? NewLocation,
    CanonicalTick Tick,
    long Sequence,
    TruthStreamIdentity StreamIdentity,
    ReadOnlyMemory<byte> PreviousHash,
    ReadOnlyMemory<byte> Hash
) : IPlateTopologyEvent
{
    /// <summary>
    /// Gets the event type discriminator for polymorphic deserialization.
    /// </summary>
    string IPlateTopologyEvent.EventType => nameof(JunctionUpdatedEvent);
}
