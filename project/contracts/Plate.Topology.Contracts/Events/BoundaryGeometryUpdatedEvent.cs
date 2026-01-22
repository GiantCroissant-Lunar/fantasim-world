using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Geometry;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Contracts.Events;

/// <summary>
/// Event representing an update to a boundary's geometric representation per FR-008.
///
/// Boundary geometries evolve due to plate motion and tectonic processes.
/// This event replaces the previous geometry with new geometry, enabling
/// temporal tracking of boundary evolution.
/// </summary>
/// <param name="EventId">Unique identifier for this event (UUIDv7).</param>
/// <param name="BoundaryId">The unique identifier of the boundary whose geometry was updated.</param>
/// <param name="NewGeometry">The new geometric representation of this boundary.</param>
/// <param name="Timestamp">When this event occurred.</param>
/// <param name="Sequence">Ordering within the event stream.</param>
/// <param name="StreamIdentity">The truth stream this event belongs to.</param>
public readonly record struct BoundaryGeometryUpdatedEvent(
    Guid EventId,
    BoundaryId BoundaryId,
    IGeometry NewGeometry,
    DateTimeOffset Timestamp,
    long Sequence,
    TruthStreamIdentity StreamIdentity
) : IPlateTopologyEvent
{
    /// <summary>
    /// Gets the event type discriminator for polymorphic deserialization.
    /// </summary>
    string IPlateTopologyEvent.EventType => nameof(BoundaryGeometryUpdatedEvent);
}
