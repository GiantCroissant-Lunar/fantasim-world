using System;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using UnifyGeometry;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Events;

/// <summary>
/// Event representing the creation of a new junction where boundaries meet per FR-008.
///
/// Junctions are the nodes of the plate boundary graph where three or more boundaries
/// converge. Junctions have a spatial location (point geometry) and track which boundaries
/// meet at that location.
/// </summary>
/// <param name="EventId">Unique identifier for this event (UUIDv7).</param>
/// <param name="JunctionId">The unique identifier of the newly created junction.</param>
/// <param name="BoundaryIds">The list of boundaries meeting at this junction.</param>
/// <param name="Location">The point geometry location of this junction.</param>
/// <param name="Tick">Canonical simulation tick when this event occurred.</param>
/// <param name="Sequence">Ordering within the event stream.</param>
/// <param name="StreamIdentity">The truth stream this event belongs to.</param>
/// <param name="PreviousHash">Hash of the previous event in the chain (empty for genesis).</param>
/// <param name="Hash">Cryptographic hash of this event (computed, not set by callers).</param>
[UnifyModel]
public readonly record struct JunctionCreatedEvent(
    [property: UnifyProperty(0)] Guid EventId,
    [property: UnifyProperty(1)] JunctionId JunctionId,
    [property: UnifyProperty(7)] BoundaryId[] BoundaryIds,
    [property: UnifyProperty(8)] Point2 Location,
    [property: UnifyProperty(2)] CanonicalTick Tick,
    [property: UnifyProperty(3)] long Sequence,
    [property: UnifyProperty(4)] TruthStreamIdentity StreamIdentity,
    [property: UnifyProperty(5)] ReadOnlyMemory<byte> PreviousHash,
    [property: UnifyProperty(6)] ReadOnlyMemory<byte> Hash
) : IPlateTopologyEvent
{
    /// <summary>
    /// Gets the event type discriminator for polymorphic deserialization.
    /// </summary>
    string IPlateTopologyEvent.EventType => nameof(JunctionCreatedEvent);
}
