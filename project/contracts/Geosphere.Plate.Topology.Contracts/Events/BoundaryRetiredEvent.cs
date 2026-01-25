using System;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Events;

/// <summary>
/// Event representing the retirement/deletion of a boundary per FR-008.
///
/// Boundary retirement marks the end of a boundary's lifecycle. Retired boundaries
/// no longer participate in the active topology but their identity remains reserved
/// to prevent reuse (per FR-005).
/// </summary>
/// <param name="EventId">Unique identifier for this event (UUIDv7).</param>
/// <param name="BoundaryId">The unique identifier of the retired boundary.</param>
/// <param name="Reason">Optional reason or cause for the retirement.</param>
/// <param name="Tick">Canonical simulation tick when this event occurred.</param>
/// <param name="Sequence">Ordering within the event stream.</param>
/// <param name="StreamIdentity">The truth stream this event belongs to.</param>
/// <param name="PreviousHash">Hash of the previous event in the chain (empty for genesis).</param>
/// <param name="Hash">Cryptographic hash of this event (computed, not set by callers).</param>
[UnifyModel]
public readonly record struct BoundaryRetiredEvent(
    [property: UnifyProperty(0)] Guid EventId,
    [property: UnifyProperty(1)] BoundaryId BoundaryId,
    [property: UnifyProperty(7)] string? Reason,
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
    string IPlateTopologyEvent.EventType => nameof(BoundaryRetiredEvent);
}
