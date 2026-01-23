using Plate.TimeDete.Time.Primitives;
using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Contracts.Events;

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
public readonly record struct JunctionCreatedEvent(
    Guid EventId,
    JunctionId JunctionId,
    BoundaryId[] BoundaryIds,
    Point2D Location,
    CanonicalTick Tick,
    long Sequence,
    TruthStreamIdentity StreamIdentity
) : IPlateTopologyEvent
{
    /// <summary>
    /// Gets the event type discriminator for polymorphic deserialization.
    /// </summary>
    string IPlateTopologyEvent.EventType => nameof(JunctionCreatedEvent);
}
