using System;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Events;

/// <summary>
/// Event representing a change to a boundary's type classification per FR-008.
///
/// Boundary types can change over time due to tectonic evolution. This event
/// tracks the transition from one type to another, preserving historical state.
/// </summary>
/// <param name="EventId">Unique identifier for this event (UUIDv7).</param>
/// <param name="BoundaryId">The unique identifier of the boundary whose type changed.</param>
/// <param name="OldType">The previous boundary type before this change.</param>
/// <param name="NewType">The new boundary type after this change.</param>
/// <param name="Tick">Canonical simulation tick when this event occurred.</param>
/// <param name="Sequence">Ordering within the event stream.</param>
/// <param name="StreamIdentity">The truth stream this event belongs to.</param>
/// <param name="PreviousHash">Hash of the previous event in the chain (empty for genesis).</param>
/// <param name="Hash">Cryptographic hash of this event (computed, not set by callers).</param>
[UnifyModel]
public readonly record struct BoundaryTypeChangedEvent(
    [property: UnifyProperty(0)] Guid EventId,
    [property: UnifyProperty(1)] BoundaryId BoundaryId,
    [property: UnifyProperty(7)] BoundaryType OldType,
    [property: UnifyProperty(8)] BoundaryType NewType,
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
    string IPlateTopologyEvent.EventType => nameof(BoundaryTypeChangedEvent);
}
