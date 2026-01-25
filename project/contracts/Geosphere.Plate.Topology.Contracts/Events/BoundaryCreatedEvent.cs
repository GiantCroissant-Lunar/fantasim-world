using System;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using UnifyGeometry;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Events;

/// <summary>
/// Event representing the creation of a new boundary between two plates per FR-008.
///
/// A boundary separates two plates in the topology. Boundaries are the fundamental
/// edges of the plate boundary graph and have both a type classification (divergent,
/// convergent, transform) and a geometric representation.
/// </summary>
/// <param name="EventId">Unique identifier for this event (UUIDv7).</param>
/// <param name="BoundaryId">The unique identifier of the newly created boundary.</param>
/// <param name="PlateIdLeft">The plate on the left side of this boundary.</param>
/// <param name="PlateIdRight">The plate on the right side of this boundary.</param>
/// <param name="BoundaryType">The type classification of this boundary.</param>
/// <param name="Geometry">The initial geometric representation of this boundary.</param>
/// <param name="Tick">Canonical simulation tick when this event occurred.</param>
/// <param name="Sequence">Ordering within the event stream.</param>
/// <param name="StreamIdentity">The truth stream this event belongs to.</param>
/// <param name="PreviousHash">Hash of the previous event in the chain (empty for genesis).</param>
/// <param name="Hash">Cryptographic hash of this event (computed, not set by callers).</param>
public readonly record struct BoundaryCreatedEvent(
    Guid EventId,
    BoundaryId BoundaryId,
    PlateId PlateIdLeft,
    PlateId PlateIdRight,
    BoundaryType BoundaryType,
    IGeometry Geometry,
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
    string IPlateTopologyEvent.EventType => nameof(BoundaryCreatedEvent);
}
