using Plate.Topology.Contracts.Entities;
using Plate.Topology.Contracts.Identity;

namespace Plate.Topology.Contracts.Events;

/// <summary>
/// Event representing the retirement of a junction per FR-008.
///
/// Junction retirement marks the end of a junction's lifecycle. Retired junctions
/// no longer participate in the active topology but their identity remains reserved
/// to prevent reuse (per FR-005).
/// </summary>
/// <param name="EventId">Unique identifier for this event (UUIDv7).</param>
/// <param name="JunctionId">The unique identifier of the retired junction.</param>
/// <param name="Reason">Optional reason or cause for the retirement.</param>
/// <param name="Timestamp">When this event occurred.</param>
/// <param name="Sequence">Ordering within the event stream.</param>
/// <param name="StreamIdentity">The truth stream this event belongs to.</param>
public readonly record struct JunctionRetiredEvent(
    Guid EventId,
    JunctionId JunctionId,
    string? Reason,
    DateTimeOffset Timestamp,
    long Sequence,
    TruthStreamIdentity StreamIdentity
) : IPlateTopologyEvent
{
    /// <summary>
    /// Gets the event type discriminator for polymorphic deserialization.
    /// </summary>
    string IPlateTopologyEvent.EventType => nameof(JunctionRetiredEvent);
}
