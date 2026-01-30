using System;
using System.Runtime.InteropServices;
using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Events;

/// <summary>
/// Event representing the creation of a new tectonic plate per FR-008.
///
/// Creation events establish the existence of a plate in the topology.
/// Once created, a plate is uniquely identified by its PlateId and persists
/// through all subsequent topology changes until retirement.
/// </summary>
/// <param name="EventId">Unique identifier for this event (UUIDv7).</param>
/// <param name="PlateId">The unique identifier of the newly created plate.</param>
/// <param name="Tick">Canonical simulation tick when this event occurred.</param>
/// <param name="Sequence">Ordering within the event stream.</param>
/// <param name="StreamIdentity">The truth stream this event belongs to.</param>
/// <param name="PreviousHash">Hash of the previous event in the chain (empty for genesis).</param>
/// <param name="Hash">Cryptographic hash of this event (computed, not set by callers).</param>
[StructLayout(LayoutKind.Auto)]
[UnifyModel]
public readonly record struct PlateCreatedEvent(
    [property: UnifyProperty(0)] Guid EventId,
    [property: UnifyProperty(1)] PlateId PlateId,
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
    string IPlateTopologyEvent.EventType => nameof(PlateCreatedEvent);
}
