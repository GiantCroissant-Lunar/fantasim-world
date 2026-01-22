using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Contracts.Events;

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
/// <param name="Timestamp">When this event occurred.</param>
/// <param name="Sequence">Ordering within the event stream.</param>
/// <param name="StreamIdentity">The truth stream this event belongs to.</param>
public readonly record struct BoundaryTypeChangedEvent(
    Guid EventId,
    BoundaryId BoundaryId,
    BoundaryType OldType,
    BoundaryType NewType,
    DateTimeOffset Timestamp,
    long Sequence,
    TruthStreamIdentity StreamIdentity
) : IPlateTopologyEvent
{
    /// <summary>
    /// Gets the event type discriminator for polymorphic deserialization.
    /// </summary>
    string IPlateTopologyEvent.EventType => nameof(BoundaryTypeChangedEvent);
}
